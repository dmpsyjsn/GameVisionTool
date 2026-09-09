using GameVisionTool.Common.Domain.Services;
using GameVisionTool.IntegrationTests.Helpers;
using GameVisionTool.Logic.Application.CommandHandlers.MainSettings;
using GameVisionTool.Logic.Domain.MainSettings;
using GameVisionTool.Messages.Commands.MainSettings;
using GameVisionTool.Persistence.LiteDb;
using Xunit;

namespace GameVisionTool.IntegrationTests.Application.CommandHandlerTests.MainSettings
{
    public class MainSettingsHandlerTests : CustomLiteDbTestDriver
    {
        private const string KnownApiLlmType = nameof(ApiLlmType.GoogleGemini);

        private readonly IDataStore<LocalLLMSetting, Guid> _localLlmDataStore;
        private readonly IDataStore<ApiLlmSetting, Guid> _apiLlmDataStore;
        private readonly MainSettingHandlers _mainSettingHandlers;

        public MainSettingsHandlerTests()
        {
            _localLlmDataStore = new LiteDbDataStore<LocalLLMSetting, Guid>(Db);
            _apiLlmDataStore = new LiteDbDataStore<ApiLlmSetting, Guid>(Db);

            _mainSettingHandlers = new MainSettingHandlers(_localLlmDataStore, _apiLlmDataStore);
        }

        #region Local LLM

        [Fact]
        public void Test_Add_Or_Update_Local_Llm_Path_Succeeds()
        {
            var id = Guid.NewGuid();
            var name = StringHelpers.GenerateRandomString();
            var path = StringHelpers.GenerateRandomString();
            var command = new AddOrUpdateLocalLlmPath(id, name, path);

            var result = _mainSettingHandlers.Handle(command);

            Assert.True(result.IsSuccess);
            Assert.Equal(id, result.Value);

            var item = _localLlmDataStore.GetByIdOrDefault(id);
            Assert.NotNull(item);
            Assert.Equal(name, item.Name);
            Assert.Equal(path, item.FullFilePath);
        }

        [Fact]
        public void Test_Add_Local_Llm_Path_Stamps_Timestamps()
        {
            var id = Guid.NewGuid();

            _mainSettingHandlers.Handle(new AddOrUpdateLocalLlmPath(
                id, StringHelpers.GenerateRandomString(), StringHelpers.GenerateRandomString()));

            var item = _localLlmDataStore.GetByIdOrDefault(id);

            Assert.NotNull(item);
            Assert.NotNull(item.CreatedOn);
            Assert.NotNull(item.ModifiedOn);
        }

        [Fact]
        public void Test_Add_Or_Update_Local_Llm_Path_With_Same_Id_Updates_Rather_Than_Duplicates()
        {
            var id = Guid.NewGuid();
            var originalName = StringHelpers.GenerateRandomString();
            var updatedName = StringHelpers.GenerateRandomString();
            var updatedPath = StringHelpers.GenerateRandomString();

            _mainSettingHandlers.Handle(new AddOrUpdateLocalLlmPath(
                id, originalName, StringHelpers.GenerateRandomString()));

            var result = _mainSettingHandlers.Handle(new AddOrUpdateLocalLlmPath(id, updatedName, updatedPath));

            Assert.True(result.IsSuccess);
            Assert.Equal(id, result.Value);
            Assert.Single(_localLlmDataStore.GetAll());

            var item = _localLlmDataStore.GetByIdOrDefault(id);
            Assert.NotNull(item);
            Assert.Equal(updatedName, item.Name);
            Assert.Equal(updatedPath, item.FullFilePath);
        }

        [Fact]
        public void Test_Add_Or_Update_Local_Llm_Path_Returns_The_Id_It_Was_Given()
        {
            // The Result<Guid> must carry the caller's id back, not a newly minted one - the
            // returned value is what a caller would use to locate or remove the entry afterwards.
            var id = Guid.NewGuid();

            var result = _mainSettingHandlers.Handle(new AddOrUpdateLocalLlmPath(
                id, StringHelpers.GenerateRandomString(), StringHelpers.GenerateRandomString()));

            Assert.True(result.IsSuccess);
            Assert.Equal(id, result.Value);
            Assert.NotEqual(Guid.Empty, result.Value);

            var removal = _mainSettingHandlers.Handle(new RemoveLocalLlmPath(result.Value));
            Assert.True(removal.IsSuccess);
        }

        [Fact]
        public void Test_Add_Or_Update_Local_Llm_Path_With_Different_Ids_Creates_Separate_Entries()
        {
            var sharedName = StringHelpers.GenerateRandomString();

            _mainSettingHandlers.Handle(new AddOrUpdateLocalLlmPath(
                Guid.NewGuid(), sharedName, StringHelpers.GenerateRandomString()));
            _mainSettingHandlers.Handle(new AddOrUpdateLocalLlmPath(
                Guid.NewGuid(), sharedName, StringHelpers.GenerateRandomString()));

            Assert.Equal(2, _localLlmDataStore.GetAll().Count());
        }

        [Fact]
        public void Test_Remove_Local_Llm_Path_Succeeds()
        {
            var id = Guid.NewGuid();
            _mainSettingHandlers.Handle(new AddOrUpdateLocalLlmPath(
                id, StringHelpers.GenerateRandomString(), StringHelpers.GenerateRandomString()));

            var result = _mainSettingHandlers.Handle(new RemoveLocalLlmPath(id));

            Assert.True(result.IsSuccess);
            Assert.Null(_localLlmDataStore.GetByIdOrDefault(id));
            Assert.Empty(_localLlmDataStore.GetAll());
        }

        [Fact]
        public void Test_Remove_Local_Llm_Path_That_Does_Not_Exist_Fails_Without_Throwing()
        {
            var result = _mainSettingHandlers.Handle(new RemoveLocalLlmPath(Guid.NewGuid()));

            Assert.True(result.IsFailure);
            Assert.Equal("The specified Local LLM path does not exist.", result.Error);
        }

        [Fact]
        public void Test_Remove_Local_Llm_Path_Leaves_Other_Entries_Intact()
        {
            var keepId = Guid.NewGuid();
            var removeId = Guid.NewGuid();

            _mainSettingHandlers.Handle(new AddOrUpdateLocalLlmPath(
                keepId, StringHelpers.GenerateRandomString(), StringHelpers.GenerateRandomString()));
            _mainSettingHandlers.Handle(new AddOrUpdateLocalLlmPath(
                removeId, StringHelpers.GenerateRandomString(), StringHelpers.GenerateRandomString()));

            _mainSettingHandlers.Handle(new RemoveLocalLlmPath(removeId));

            Assert.Single(_localLlmDataStore.GetAll());
            Assert.NotNull(_localLlmDataStore.GetByIdOrDefault(keepId));
        }

        #endregion

        #region API LLM

        [Fact]
        public void Test_Add_Or_Update_Api_Setting_Succeeds()
        {
            var apiKey = StringHelpers.GenerateRandomString();
            var apiUrl = StringHelpers.GenerateRandomString();

            var result = _mainSettingHandlers.Handle(new AddOrUpdateApiSetting(KnownApiLlmType, apiKey, apiUrl));

            Assert.True(result.IsSuccess);

            var item = Assert.Single(_apiLlmDataStore.GetAll());
            Assert.Equal(KnownApiLlmType, item.Type);
            Assert.Equal(apiKey, item.ApiKey);
            Assert.Equal(apiUrl, item.ApiUrl);
        }

        [Fact]
        public void Test_Add_Or_Update_Api_Setting_With_Same_Type_Updates_Rather_Than_Duplicates()
        {
            var updatedKey = StringHelpers.GenerateRandomString();
            var updatedUrl = StringHelpers.GenerateRandomString();

            _mainSettingHandlers.Handle(new AddOrUpdateApiSetting(
                KnownApiLlmType, StringHelpers.GenerateRandomString(), StringHelpers.GenerateRandomString()));

            var result = _mainSettingHandlers.Handle(new AddOrUpdateApiSetting(KnownApiLlmType, updatedKey, updatedUrl));

            Assert.True(result.IsSuccess);

            var item = Assert.Single(_apiLlmDataStore.GetAll());
            Assert.Equal(updatedKey, item.ApiKey);
            Assert.Equal(updatedUrl, item.ApiUrl);
        }

        [Theory]
        [InlineData("NotARealProvider")]   // not a member of the enum at all
        [InlineData("googlegemini")]       // correct name, wrong case - matching is case-sensitive
        [InlineData("GOOGLEGEMINI")]
        [InlineData("12345")]              // numeric string outside the enum's range
        [InlineData("0")]                  // numeric string that maps onto a DEFINED member
        [InlineData("")]
        [InlineData("   ")]
        public void Test_Add_Or_Update_Api_Setting_With_Unrecognised_Type_Fails(string apiLlmType)
        {
            var result = _mainSettingHandlers.Handle(new AddOrUpdateApiSetting(
                apiLlmType, StringHelpers.GenerateRandomString(), StringHelpers.GenerateRandomString()));

            Assert.True(result.IsFailure);
            Assert.Equal("The specified API LLM type is not recognized.", result.Error);
            Assert.Empty(_apiLlmDataStore.GetAll());
        }

        [Fact]
        public void Test_Remove_Api_Setting_Succeeds()
        {
            _mainSettingHandlers.Handle(new AddOrUpdateApiSetting(
                KnownApiLlmType, StringHelpers.GenerateRandomString(), StringHelpers.GenerateRandomString()));

            var stored = Assert.Single(_apiLlmDataStore.GetAll());

            var result = _mainSettingHandlers.Handle(new RemoveApiSetting(stored.Id));

            Assert.True(result.IsSuccess);
            Assert.Empty(_apiLlmDataStore.GetAll());
        }

        [Fact]
        public void Test_Remove_Api_Setting_That_Does_Not_Exist_Fails_Without_Throwing()
        {
            var result = _mainSettingHandlers.Handle(new RemoveApiSetting(Guid.NewGuid()));

            Assert.True(result.IsFailure);
            Assert.Equal("The specified API setting does not exist.", result.Error);
        }

        #endregion

        #region Isolation

        [Fact]
        public void Test_Local_Llm_And_Api_Settings_Are_Stored_Independently()
        {
            _mainSettingHandlers.Handle(new AddOrUpdateLocalLlmPath(
                Guid.NewGuid(), StringHelpers.GenerateRandomString(), StringHelpers.GenerateRandomString()));
            _mainSettingHandlers.Handle(new AddOrUpdateApiSetting(
                KnownApiLlmType, StringHelpers.GenerateRandomString(), StringHelpers.GenerateRandomString()));

            Assert.Single(_localLlmDataStore.GetAll());
            Assert.Single(_apiLlmDataStore.GetAll());
        }

        #endregion
    }
}
