using System;
using System.IO.Abstractions;
using System.IO.Abstractions.TestingHelpers;
using FakeItEasy;
using FluentAssertions;
using FluentTc.Engine;
using FluentTc.Exceptions;
using FluentTc.Tests.Locators;
using FluentTc.Tests.TestingTools;
using JetBrains.TeamCity.ServiceMessages.Write.Special;
using NUnit.Framework;

namespace FluentTc.Tests.Engine
{
    [TestFixture]
    public class BuildParametersTests
    {
        [Test]
        public void GetParameter_File_Parsed()
        {
            // Arrange
            var mockFileSystem = new MockFileSystem();
            var propertiesFile = @"C:\BuildAgent\temp\buildTmp\teamcity.build322130465402584030.properties";
            mockFileSystem.AddFile(propertiesFile, @"#TeamCity build properties without 'system.' prefix
#Sun Nov 01 14:40:00 IST 2015
agent.home.dir=C\:\\BuildAgent
");

            var teamCityContext = new BuildParameters(propertiesFile, mockFileSystem, A.Fake<ITeamCityWriterFactory>());

            // Act + Assert
            teamCityContext.GetParameterValue("agent.home.dir").Should().Be(@"C:\BuildAgent");
            teamCityContext.AgentHomeDir.Should().Be(@"C:\BuildAgent");
        }

        [Test]
        public void Constructor_EnvironmentVariableIsNull_NoExceptionThrown()
        {
            // Arrange
            var fileSystem = new MockFileSystem();

            // Act
            Action action = () => new BuildParameters(null, fileSystem, A.Fake<ITeamCityWriterFactory>());

            // Assert
            action.ShouldNotThrow<Exception>();
        }

        [Test]
        public void GetParameter_RealFile_Parsed()
        {
            string resource = new EmbeddedResourceReader().GetResource("PropertiesFile.txt");

            // Arrange
            var mockFileSystem = new MockFileSystem();
            var propertiesFile = @"C:\BuildAgent\temp\buildTmp\teamcity.build322130465402584030.properties";
            mockFileSystem.AddFile(propertiesFile, resource);

            var teamCityContext = new BuildParameters(propertiesFile, mockFileSystem, A.Fake<ITeamCityWriterFactory>());

            // Act + Assert
            teamCityContext.AgentHomeDir.Should().Be(@"C:\BuildAgent");
            teamCityContext.AgentName.Should().Be(@"BUILDS8");
            teamCityContext.AgentOwnPort.Should().Be(@"9090");
            teamCityContext.AgentWorkDir.Should().Be(@"C:\BuildAgent\work");
            teamCityContext.BuildNumber.Should().Be(@"4");
            teamCityContext.GetParameterValue("FxCopRoot").Should().Be(@"C:\Program Files (x86)\Microsoft Visual Studio 12.0\Team Tools\Static Analysis Tools\FxCop");
            teamCityContext.GetParameterValue("teamcity.agent.dotnet.agent_url").Should().Be(@"http://localhost:9090/RPC2");
            teamCityContext.GetParameterValue("teamcity.auth.userId").Should().Be(@"TeamCityBuildId=781682");
        }

        [Test]
        public void SetParameterValue_NotTeamCityMode_ValueSet()
        {
            var mockFileSystem = new MockFileSystem();
            var propertiesFile = @"C:\BuildAgent\temp\buildTmp\teamcity.build322130465402584030.properties";
            mockFileSystem.AddFile(propertiesFile, MockFileData.NullObject);

            var teamCityWriter = A.Fake<ITeamCityWriter>();

            var teamCityWriterFactory = A.Fake<ITeamCityWriterFactory>();
            A.CallTo(() => teamCityWriterFactory.CreateTeamCityWriter()).Returns(teamCityWriter);

            var buildParameters = new BuildParameters(propertiesFile, mockFileSystem, teamCityWriterFactory);
            buildParameters.SetParameterValue("param1", "newValue");
            var parameterValue = buildParameters.GetParameterValue("param1");

            // Assert
            parameterValue.Should().Be("newValue");
            A.CallTo(() => teamCityWriter.WriteBuildParameter("param1", "newValue")).MustHaveHappened();
        }        
        
        [Test]
        public void SetParameterValue_GetParameterValue_ValueThatWasSetReturned()
        {
            var mockFileSystem = new MockFileSystem();
            var propertiesFile = @"C:\BuildAgent\temp\buildTmp\teamcity.build322130465402584030.properties";
            mockFileSystem.AddFile(propertiesFile, MockFileData.NullObject);

            var teamCityWriter = A.Fake<ITeamCityWriter>();

            var teamCityWriterFactory = A.Fake<ITeamCityWriterFactory>();
            A.CallTo(() => teamCityWriterFactory.CreateTeamCityWriter()).Returns(teamCityWriter);

            var buildParameters = new BuildParameters(propertiesFile, mockFileSystem, teamCityWriterFactory);

            // Act
            buildParameters.SetParameterValue("param1", "newValue");
            var parameterValue = buildParameters.GetParameterValue("param1");

            // Assert
            parameterValue.Should().Be("newValue");
        }

        [Test]
        public void SetParameterValue_NotTeamCityMode_ExceptionThrown()
        {
            var buildParameters = new BuildParameters(null, A.Fake<IFileSystem>(), A.Fake<ITeamCityWriterFactory>());
            Action action = () => buildParameters.SetParameterValue("param1", "newValue");

            // Assert
            action.ShouldThrow<MissingBuildParameterException>();
        }
    }
}