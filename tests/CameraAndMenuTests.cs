using System.Collections.Generic;
using RoRCheatPanel.Channel;
using Xunit;

namespace RoRCheatPanel.Channel.Tests
{
    public class CameraAndMenuTests
    {
        [Fact]
        public void NormalSizeResetsCameraDistance()
        {
            Assert.Equal(0f, CameraFraming.DistanceForBody(1f));
            Assert.Equal(0f, CameraFraming.DistanceForBody(float.NaN));
        }

        [Fact]
        public void SizeButtonsPickAPlayableSliderDistance()
        {
            Assert.Equal(2.44f, CameraFraming.DistanceForBody(0.35f), 2);
            Assert.Equal(7.48f, CameraFraming.DistanceForBody(3.2f), 2);
            Assert.Equal(17f, CameraFraming.DistanceForBody(6f), 2);
            Assert.Equal(30f, CameraFraming.ClampDistance(80f));
            Assert.Equal(0f, CameraFraming.ClampDistance(-3f));
        }

        [Fact]
        public void TeamVerbChangesOnlyForOtherPlayers()
        {
            Assert.Equal("Стать: Монстры", TeamWords.Apply("Стать: Монстры", false));
            Assert.Equal("Сделать: Монстры", TeamWords.Apply("Стать: Монстры", true));
            Assert.Equal("Make: Monsters", TeamWords.Apply("Join: Monsters", true));
            Assert.Equal("Hacer: Void", TeamWords.Apply("Unirse: Void", true));
            Assert.Equal("Спавн", TeamWords.Apply("Спавн", true));
        }

        [Fact]
        public void GiveAllVisibleAndDuplicateJokesStayHidden()
        {
            var seen = new HashSet<string>();
            var frame = -1;
            Assert.True(MenuFilter.Drop("Выдать все видимые", 1, ref frame, seen));
            Assert.True(MenuFilter.Drop("Give all visible", 1, ref frame, seen));
            Assert.True(MenuFilter.Drop("Пьяная камера", 1, ref frame, seen));
            Assert.False(MenuFilter.Drop("Всех ко мне", 1, ref frame, seen));
            Assert.True(MenuFilter.Drop("Всех ко мне", 1, ref frame, seen));
            Assert.False(MenuFilter.Drop("Случайный предмет", 2, ref frame, seen));
        }

        [Theory]
        [InlineData(1, 2)]
        [InlineData(2, 2)]
        [InlineData(4, 4)]
        [InlineData(64, 64)]
        [InlineData(99, 64)]
        public void LobbySizeStaysBetweenTwoAndSixtyFour(int requested, int expected)
        {
            Assert.Equal(expected, LobbyRules.Clamp(requested));
        }
    }
}
