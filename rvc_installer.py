# -*- coding: utf-8 -*-
"""Standalone installer for the official RVC WebUI.

Double-click the built exe. It checks Python, downloads the official repo,
fetches the NVIDIA package linked from the latest GitHub release, installs
pip requirements, and starts the WebUI. It does not train a voice model.

PyInstaller executes this file while building. The build sets
RVC_INSTALLER_BUILDING=1 so the window and the install do not start.
"""

from __future__ import annotations

import ctypes
import json
import os
import queue
import re
import shutil
import subprocess
import sys
import threading
import urllib.error
import urllib.request
import zipfile

DEST = r"C:\RVC"
REPO = "https://github.com/RVC-Project/Retrieval-based-Voice-Conversion-WebUI"
REPO_GIT = REPO + ".git"
REPO_ZIP = REPO + "/archive/refs/heads/main.zip"
RELEASE_API = (
    "https://api.github.com/repos/RVC-Project/"
    "Retrieval-based-Voice-Conversion-WebUI/releases/latest"
)
# Used only when the latest release page cannot be read. These are the
# NVIDIA links published in release 2.3.260718.
FALLBACK_NVIDIA = (
    "https://huggingface.co/lj1995/VoiceConversionWebUI/resolve/main/"
    "RVC20260718Nvidia.7z"
)
FALLBACK_NVIDIA_50 = (
    "https://huggingface.co/lj1995/VoiceConversionWebUI/resolve/main/"
    "RVC20260718Nvidia50x0.7z"
)
PYTHON_INSTALLER = (
    "https://www.python.org/ftp/python/3.12.10/python-3.12.10-amd64.exe"
)
MIN_FREE_GB = 30
NO_WINDOW = 0x08000000
USER_AGENT = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) RVC-Installer"


class InstallError(Exception):
    pass


def _request(url: str) -> urllib.request.Request:
    return urllib.request.Request(url, headers={"User-Agent": USER_AGENT})


def download(url: str, dest: str, on_progress) -> None:
    tmp = dest + ".part"
    os.makedirs(os.path.dirname(dest), exist_ok=True)
    try:
        with urllib.request.urlopen(_request(url), timeout=120) as resp:
            total = int(resp.headers.get("Content-Length") or 0)
            done = 0
            last_pct = -1
            with open(tmp, "wb") as out:
                while True:
                    chunk = resp.read(1024 * 256)
                    if not chunk:
                        break
                    out.write(chunk)
                    done += len(chunk)
                    pct = int(done * 100 / total) if total else 0
                    if pct != last_pct:
                        last_pct = pct
                        on_progress(pct, done, total)
        os.replace(tmp, dest)
    except Exception:
        if os.path.exists(tmp):
            try:
                os.remove(tmp)
            except OSError:
                pass
        raise


def run_stream(cmd: list[str], cwd: str | None, log) -> None:
    proc = subprocess.Popen(
        cmd,
        cwd=cwd,
        stdout=subprocess.PIPE,
        stderr=subprocess.STDOUT,
        text=True,
        encoding="utf-8",
        errors="replace",
        creationflags=NO_WINDOW,
    )
    assert proc.stdout is not None
    for line in proc.stdout:
        text = line.rstrip()
        if text:
            log(text)
    code = proc.wait()
    if code != 0:
        raise InstallError(f"Команда завершилась с кодом {code}: {cmd[0]}")


def python_version(exe: str) -> tuple[int, int, int] | None:
    try:
        result = subprocess.run(
            [exe, "-c", "import sys; print('%d.%d.%d' % sys.version_info[:3])"],
            capture_output=True,
            text=True,
            timeout=30,
            creationflags=NO_WINDOW,
        )
    except (OSError, subprocess.TimeoutExpired):
        return None
    if result.returncode != 0:
        return None
    parts = result.stdout.strip().split(".")
    if len(parts) != 3:
        return None
    try:
        return tuple(int(part) for part in parts)  # type: ignore[return-value]
    except ValueError:
        return None


def _candidate_pythons() -> list[str]:
    found: list[str] = []
    local = os.environ.get("LOCALAPPDATA", "")
    roots = []
    if local:
        roots.append(os.path.join(local, "Programs", "Python"))
    roots.extend(
        [
            r"C:\Program Files\Python312",
            r"C:\Program Files\Python311",
            r"C:\Program Files\Python310",
            r"C:\Python312",
            r"C:\Python311",
            r"C:\Python310",
        ]
    )
    for root in roots:
        if root.endswith(".exe"):
            found.append(root)
            continue
        if not os.path.isdir(root):
            # A version folder such as Python312, or a parent of those folders.
            direct = os.path.join(root, "python.exe")
            if os.path.isfile(direct):
                found.append(direct)
            continue
        direct = os.path.join(root, "python.exe")
        if os.path.isfile(direct):
            found.append(direct)
        try:
            for name in os.listdir(root):
                exe = os.path.join(root, name, "python.exe")
                if os.path.isfile(exe):
                    found.append(exe)
        except OSError:
            pass

    for launcher in (["py", "-3.12"], ["py", "-3.11"], ["py", "-3.10"], ["py", "-3"]):
        if shutil.which(launcher[0]) is None and launcher[0] != "py":
            continue
        try:
            result = subprocess.run(
                launcher + ["-c", "import sys; print(sys.executable)"],
                capture_output=True,
                text=True,
                timeout=30,
                creationflags=NO_WINDOW,
            )
        except (OSError, subprocess.TimeoutExpired):
            continue
        exe = (result.stdout or "").strip()
        if result.returncode == 0 and exe:
            found.append(exe)

    which = shutil.which("python")
    if which:
        found.append(which)
    return found


def find_python() -> tuple[str, tuple[int, int, int]] | None:
    seen: set[str] = set()
    matches: list[tuple[tuple[int, int, int], str]] = []
    for exe in _candidate_pythons():
        norm = os.path.normcase(os.path.abspath(exe))
        if norm in seen or "windowsapps" in norm:
            continue
        seen.add(norm)
        version = python_version(exe)
        if version is None or version < (3, 10):
            continue
        matches.append((version, exe))
    if not matches:
        return None
    # Prefer 3.12: the official requirements files are written for it.
    matches.sort(key=lambda item: item[0])
    py312 = [item for item in matches if item[0][:2] == (3, 12)]
    version, exe = (py312 or matches)[-1]
    return exe, version


def install_python(status, log) -> tuple[str, tuple[int, int, int]]:
    status("Скачиваю Python 3.12")
    log("Python 3.10 или новее не найден. Скачиваю официальный установщик Python 3.12.")
    installer = os.path.join(os.environ.get("TEMP", r"C:\Windows\Temp"), "python-3.12.10-amd64.exe")
    download(
        PYTHON_INSTALLER,
        installer,
        lambda pct, done, total: status(f"Скачиваю Python 3.12: {pct}%"),
    )
    status("Устанавливаю Python 3.12")
    log("Устанавливаю Python без лишних вопросов. Это может занять несколько минут.")
    result = subprocess.run(
        [
            installer,
            "/quiet",
            "InstallAllUsers=0",
            "PrependPath=1",
            "Include_pip=1",
            "Include_test=0",
            "Include_launcher=1",
            "Shortcuts=0",
        ],
        timeout=900,
        creationflags=NO_WINDOW,
    )
    if result.returncode != 0:
        raise InstallError(
            "Не удалось установить Python. Код "
            f"{result.returncode}. Установите Python 3.12 с python.org и запустите программу снова."
        )
    found = find_python()
    if found is None:
        raise InstallError(
            "Python установился, но программа его не видит. Закройте установщик и запустите его ещё раз."
        )
    log(f"Python установлен: {found[1][0]}.{found[1][1]}.{found[1][2]}")
    return found


def ensure_python(status, log) -> tuple[str, tuple[int, int, int]]:
    status("Проверяю Python")
    log("Проверяю, установлен ли Python 3.10 или новее.")
    found = find_python()
    if found is not None:
        exe, version = found
        log(f"Python уже установлен: {version[0]}.{version[1]}.{version[2]} ({exe})")
        if version[:2] != (3, 12):
            log(
                "Официальные файлы зависимостей RVC рассчитаны на Python 3.12. "
                "Будет использован уже установленный Python."
            )
        return found
    return install_python(status, log)


def fetch_repo(status, log) -> None:
    os.makedirs(DEST, exist_ok=True)
    webui = os.path.join(DEST, "webui.py")
    git = shutil.which("git")
    if os.path.isfile(webui) and os.path.isdir(os.path.join(DEST, ".git")) and git:
        status("Обновляю RVC")
        log("Папка C:\\RVC уже содержит RVC. Обновляю её через Git.")
        try:
            run_stream([git, "-C", DEST, "pull", "--ff-only"], None, log)
            return
        except InstallError as exc:
            log(f"Обновление через Git не удалось ({exc}). Продолжаю с уже скачанными файлами.")
            return
    if os.path.isfile(webui):
        log("Файлы RVC уже лежат в C:\\RVC. Повторно репозиторий не скачиваю.")
        return

    if git:
        status("Скачиваю RVC через Git")
        log("Git найден. Клонирую официальный репозиторий в C:\\RVC.")
        target = DEST if not os.listdir(DEST) else os.path.join(DEST, "_src")
        try:
            run_stream([git, "clone", "--depth", "1", REPO_GIT, target], None, log)
            if target != DEST:
                _hoist(target, DEST)
                shutil.rmtree(target, ignore_errors=True)
            return
        except InstallError as exc:
            log(f"Git не смог скачать репозиторий ({exc}). Скачиваю ZIP.")
            shutil.rmtree(target, ignore_errors=True)

    status("Скачиваю ZIP репозитория")
    log("Git не найден или не сработал. Скачиваю ZIP официального репозитория.")
    zip_path = os.path.join(DEST, "_repo.zip")
    download(
        REPO_ZIP,
        zip_path,
        lambda pct, done, total: status(f"Скачиваю репозиторий: {pct}%"),
    )
    status("Распаковываю репозиторий")
    log("Распаковываю ZIP в C:\\RVC.")
    extract_root = os.path.join(DEST, "_zip")
    if os.path.isdir(extract_root):
        shutil.rmtree(extract_root)
    os.makedirs(extract_root, exist_ok=True)
    with zipfile.ZipFile(zip_path) as archive:
        archive.extractall(extract_root)
    _hoist(extract_root, DEST)
    shutil.rmtree(extract_root, ignore_errors=True)
    os.remove(zip_path)
    if not os.path.isfile(webui):
        raise InstallError("Репозиторий скачан, но webui.py в C:\\RVC не найден.")
    log("Репозиторий сохранён в C:\\RVC.")


def _hoist(folder: str, dest: str) -> None:
    """Move extracted files into dest. A single wrapping folder is removed."""
    entries = [name for name in os.listdir(folder) if name not in {".", ".."}]
    source = folder
    if len(entries) == 1 and os.path.isdir(os.path.join(folder, entries[0])):
        source = os.path.join(folder, entries[0])
    os.makedirs(dest, exist_ok=True)
    for name in os.listdir(source):
        src = os.path.join(source, name)
        target = os.path.join(dest, name)
        if os.path.isdir(src) and os.path.isdir(target):
            _hoist(src, target)
            shutil.rmtree(src, ignore_errors=True)
            continue
        if os.path.exists(target):
            if os.path.isdir(target):
                shutil.rmtree(target)
            else:
                os.remove(target)
        os.replace(src, target)


def gpu_is_50_series() -> bool:
    try:
        result = subprocess.run(
            ["nvidia-smi", "--query-gpu=name", "--format=csv,noheader"],
            capture_output=True,
            text=True,
            timeout=20,
            creationflags=NO_WINDOW,
        )
    except (OSError, subprocess.TimeoutExpired):
        return False
    text = result.stdout or ""
    return any(token in text for token in ("5090", "5080", "5070", "5060"))


def nvidia_package_url(log) -> str:
    status_note = "Ищу готовый пакет NVIDIA в последнем релизе GitHub."
    log(status_note)
    want_50 = gpu_is_50_series()
    if want_50:
        log("Обнаружена видеокарта серии RTX 50. Будет скачан пакет для неё.")
    else:
        log("Будет скачан обычный пакет NVIDIA (не серия RTX 50 и не AMD/Intel).")
    try:
        with urllib.request.urlopen(_request(RELEASE_API), timeout=60) as resp:
            data = json.loads(resp.read().decode("utf-8"))
    except (OSError, urllib.error.URLError, json.JSONDecodeError, TimeoutError) as exc:
        log(f"Страница релизов недоступна ({exc}). Использую известную ссылку пакета NVIDIA.")
        return FALLBACK_NVIDIA_50 if want_50 else FALLBACK_NVIDIA

    urls: list[str] = []
    for asset in data.get("assets") or []:
        url = asset.get("browser_download_url") or ""
        if url:
            urls.append(url)
    urls.extend(re.findall(r"https://\S+?\.7z", data.get("body") or ""))

    def pick(predicate) -> str | None:
        for url in urls:
            name = url.rsplit("/", 1)[-1].lower()
            if name.endswith(".7z") and predicate(name):
                return url
        return None

    if want_50:
        chosen = pick(lambda name: "nvidia" in name and "50" in name)
    else:
        chosen = pick(lambda name: "nvidia" in name and "50" not in name and "amd" not in name)
    if chosen:
        log(f"Ссылка из релиза: {chosen}")
        return chosen
    log("В релизе нет отдельного файла пакета. Использую ссылку NVIDIA, указанную в описании релиза.")
    return FALLBACK_NVIDIA_50 if want_50 else FALLBACK_NVIDIA


def find_7z() -> str | None:
    for path in (
        shutil.which("7z"),
        r"C:\Program Files\7-Zip\7z.exe",
        r"C:\Program Files (x86)\7-Zip\7z.exe",
    ):
        if path and os.path.isfile(path):
            return path
    return None


def ensure_7z(python_exe: str, log) -> str | None:
    found = find_7z()
    if found:
        return found
    winget = shutil.which("winget")
    if winget:
        log("7-Zip не найден. Пытаюсь установить его тихо через winget.")
        try:
            run_stream(
                [
                    winget,
                    "install",
                    "--id",
                    "7zip.7zip",
                    "-e",
                    "--silent",
                    "--accept-package-agreements",
                    "--accept-source-agreements",
                    "--disable-interactivity",
                ],
                None,
                log,
            )
        except InstallError as exc:
            log(f"winget не установил 7-Zip ({exc}).")
        found = find_7z()
        if found:
            return found
    log("Ставлю запасной распаковщик py7zr через pip.")
    run_stream([python_exe, "-m", "pip", "install", "py7zr"], None, log)
    return None


def extract_7z(archive: str, python_exe: str, seven: str | None, log) -> None:
    extract_root = os.path.join(DEST, "_pkg")
    if os.path.isdir(extract_root):
        shutil.rmtree(extract_root)
    os.makedirs(extract_root, exist_ok=True)
    if seven:
        log(f"Распаковываю архив программой {seven}.")
        run_stream([seven, "x", "-y", f"-o{extract_root}", archive], None, log)
    else:
        log("Распаковываю архив через py7zr. Для большого пакета это займёт время.")
        helper = os.path.join(os.environ.get("TEMP", DEST), "rvc_extract_7z.py")
        with open(helper, "w", encoding="utf-8") as handle:
            handle.write(
                "import py7zr, sys\n"
                "with py7zr.SevenZipFile(sys.argv[1], mode='r') as archive:\n"
                "    archive.extractall(path=sys.argv[2])\n"
            )
        run_stream([python_exe, helper, archive, extract_root], None, log)
    _hoist(extract_root, DEST)
    shutil.rmtree(extract_root, ignore_errors=True)


def fetch_nvidia_package(python_exe: str, status, log) -> None:
    free_gb = shutil.disk_usage("C:\\").free / (1024 ** 3)
    if free_gb < MIN_FREE_GB:
        raise InstallError(
            f"На диске C: свободно только {free_gb:.1f} ГБ. "
            f"Для пакета NVIDIA нужно не меньше {MIN_FREE_GB} ГБ."
        )
    url = nvidia_package_url(log)
    archive = os.path.join(DEST, "_download", os.path.basename(url.split("?", 1)[0]))
    status("Скачиваю пакет NVIDIA")
    log("Скачиваю готовый пакет NVIDIA. Файл около 7 ГБ, дождитесь окончания.")

    def progress(pct, done, total):
        if total:
            status(f"Скачиваю пакет NVIDIA: {pct}%")
        else:
            status(f"Скачиваю пакет NVIDIA: {done / (1024 ** 3):.1f} ГБ")

    download(url, archive, progress)
    status("Распаковываю пакет NVIDIA")
    log("Распаковываю пакет NVIDIA в C:\\RVC.")
    seven = ensure_7z(python_exe, log)
    extract_7z(archive, python_exe, seven, log)
    try:
        os.remove(archive)
    except OSError:
        pass
    log("Пакет NVIDIA распакован.")


def requirements_file() -> str | None:
    # Upstream spells this filename without the second "e".
    preferred = [
        "requirments_cu128_py312.txt",
        "requirments_cu118_py312.txt",
        "requirements.txt",
    ]
    for name in preferred:
        path = os.path.join(DEST, name)
        if os.path.isfile(path):
            return path
    return None


def install_requirements(python_exe: str, status, log) -> None:
    req = requirements_file()
    if req is None:
        raise InstallError("В C:\\RVC не найден файл зависимостей pip.")
    status("Устанавливаю зависимости")
    log(f"Устанавливаю зависимости из {os.path.basename(req)}. Это может занять долго.")
    run_stream([python_exe, "-m", "pip", "install", "--upgrade", "pip"], None, log)
    run_stream([python_exe, "-m", "pip", "install", "-r", req], None, log)
    log("Зависимости установлены.")


def launch_webui(python_exe: str, status, log) -> None:
    status("Запускаю WebUI")
    bat = os.path.join(DEST, "go-webui.bat")
    runtime = os.path.join(DEST, "runtime", "python.exe")
    if os.path.isfile(bat):
        log("Запускаю go-webui.bat. Не закрывайте чёрное окно RVC.")
        os.startfile(bat)  # type: ignore[attr-defined]
        if os.path.isfile(runtime):
            log("Готовый пакет использует свой Python: C:\\RVC\\runtime\\python.exe")
        log("Обычно страница открывается по адресу http://127.0.0.1:7897")
        return
    webui = os.path.join(DEST, "webui.py")
    if not os.path.isfile(webui):
        raise InstallError("Не найден go-webui.bat и не найден webui.py. Запускать нечего.")
    log("Готовый bat-файл не найден. Запускаю webui.py установленным Python.")
    subprocess.Popen(
        [python_exe, webui],
        cwd=DEST,
        creationflags=subprocess.CREATE_NEW_CONSOLE,
    )
    log("Обычно страница открывается по адресу http://127.0.0.1:7865")


def install(status, log) -> None:
    log("Установка идёт в C:\\RVC. Модели голоса эта программа не обучает.")
    python_exe, _version = ensure_python(status, log)
    fetch_repo(status, log)
    fetch_nvidia_package(python_exe, status, log)
    try:
        install_requirements(python_exe, status, log)
    except InstallError as exc:
        bat = os.path.join(DEST, "go-webui.bat")
        if not os.path.isfile(bat):
            raise
        log(f"Зависимости системного Python не установились: {exc}")
        log("Запускаю готовую сборку NVIDIA: в ней уже есть свой Python и библиотеки.")
    launch_webui(python_exe, status, log)
    status("Готово")
    log("Готово. Это окно можно закрыть. Окно RVC закройте только когда закончите работу.")


def _hold_single_instance() -> bool:
    kernel32 = ctypes.WinDLL("kernel32", use_last_error=True)
    handle = kernel32.CreateMutexW(None, False, "Local\\RVCWebUIInstaller")
    if not handle:
        return True
    if ctypes.get_last_error() == 183:
        return False
    # Keep the handle alive for the life of the process.
    globals()["_INSTALLER_MUTEX"] = handle
    return True


def main() -> None:
    import tkinter as tk
    from tkinter import scrolledtext
    from tkinter import messagebox

    if not _hold_single_instance():
        root = tk.Tk()
        root.withdraw()
        messagebox.showinfo("Установщик RVC", "Установщик уже запущен.")
        root.destroy()
        return

    root = tk.Tk()
    root.title("Установщик RVC")
    root.geometry("760x520")
    root.minsize(640, 420)
    root.configure(bg="#f4f6f8")

    status_var = tk.StringVar(value="Подготовка")
    header = tk.Label(
        root,
        text="Установщик RVC WebUI",
        bg="#f4f6f8",
        fg="#14213d",
        font=("Segoe UI", 16, "bold"),
        anchor="w",
    )
    header.pack(fill="x", padx=16, pady=(14, 0))
    status = tk.Label(
        root,
        textvariable=status_var,
        bg="#f4f6f8",
        fg="#1d4e89",
        font=("Segoe UI", 12),
        anchor="w",
    )
    status.pack(fill="x", padx=16, pady=(4, 8))
    log_box = scrolledtext.ScrolledText(
        root,
        font=("Segoe UI", 10),
        wrap="word",
        bg="white",
        fg="#1b1b1b",
        relief="flat",
        padx=8,
        pady=8,
    )
    log_box.pack(fill="both", expand=True, padx=16, pady=(0, 8))
    log_box.configure(state="disabled")

    messages: queue.Queue[tuple[str, str]] = queue.Queue()
    finished = {"ok": False}

    def write_log(text: str) -> None:
        log_box.configure(state="normal")
        log_box.insert("end", text + "\n")
        log_box.see("end")
        log_box.configure(state="disabled")
        try:
            log_path = os.path.join(os.environ.get("TEMP", r"C:\Windows\Temp"), "RVC-install-log.txt")
            with open(log_path, "a", encoding="utf-8") as handle:
                handle.write(text + "\n")
        except OSError:
            pass

    def pump() -> None:
        try:
            while True:
                kind, text = messages.get_nowait()
                if kind == "status":
                    status_var.set(text)
                else:
                    write_log(text)
        except queue.Empty:
            pass
        if not finished["done"]:
            root.after(150, pump)

    finished["done"] = False

    def worker() -> None:
        try:
            install(
                lambda text: messages.put(("status", text)),
                lambda text: messages.put(("log", text)),
            )
            finished["ok"] = True
        except Exception as exc:
            messages.put(("status", "Ошибка"))
            messages.put(("log", f"ОШИБКА: {exc}"))
        finally:
            finished["done"] = True

    def on_close() -> None:
        if finished["done"]:
            root.destroy()
            return
        messagebox.showinfo(
            "Установщик RVC",
            "Установка ещё идёт. Дождитесь надписи «Готово» или «Ошибка».",
        )

    close_button = tk.Button(
        root,
        text="Закрыть",
        font=("Segoe UI", 10),
        command=on_close,
        state="disabled",
    )
    close_button.pack(pady=(0, 14))
    root.protocol("WM_DELETE_WINDOW", on_close)

    def watch_button() -> None:
        if finished["done"]:
            close_button.configure(state="normal")
            return
        root.after(200, watch_button)

    threading.Thread(target=worker, daemon=True).start()
    root.after(150, pump)
    root.after(200, watch_button)
    root.mainloop()


if __name__ == "__main__" and os.environ.get("RVC_INSTALLER_BUILDING") != "1":
    main()
