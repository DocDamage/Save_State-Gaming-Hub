namespace SaveState.Presentation.UITests;

using System.Globalization;
using Microsoft.Extensions.Localization;
using Moq;
using SaveState.Presentation.Resources;

/// <summary>
/// Creates deterministic localized resources for UI tests.
/// </summary>
internal static class UiTestResourceFactory
{
    public static Resources Create()
    {
        var localizerMock = new Mock<IStringLocalizer<Resources>>();
        localizerMock
            .Setup(l => l[It.IsAny<string>()])
            .Returns((string key) => new LocalizedString(key, key));
        localizerMock
            .Setup(l => l[It.IsAny<string>(), It.IsAny<object[]>()])
            .Returns((string key, object[] _) => new LocalizedString(key, key));
        localizerMock
            .Setup(l => l.GetAllStrings(It.IsAny<bool>()))
            .Returns(Enumerable.Empty<LocalizedString>());

        return new Resources(localizerMock.Object);
    }
}
