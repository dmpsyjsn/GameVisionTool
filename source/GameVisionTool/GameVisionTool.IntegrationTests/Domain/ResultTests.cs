using GameVisionTool.Common.Domain.Services;
using Xunit;

namespace GameVisionTool.IntegrationTests.Domain
{
    public class ResultTests
    {
        [Fact]
        public void Test_Reading_Value_On_A_Failed_Reference_Result_Returns_Default()
        {
            // By design: the failure is already reported through IsFailure/Error, so Value does not
            // throw. Callers are expected to check the flag first.
            var result = Result.Fail<string>("db unavailable");

            Assert.Null(result.Value);
        }

        [Fact]
        public void Test_Reading_Value_On_A_Failed_Value_Type_Result_Returns_Default()
        {
            var result = Result.Fail<int>("db unavailable");

            Assert.Equal(0, result.Value);
        }

        [Fact]
        public void Test_Reading_Value_On_A_Successful_Result_Returns_The_Value()
        {
            var result = Result.Ok("all good");

            Assert.Equal("all good", result.Value);
        }

        [Fact]
        public void Test_Failure_Carries_Its_Error_Message()
        {
            var result = Result.Fail<string>("db unavailable");

            Assert.True(result.IsFailure);
            Assert.False(result.IsSuccess);
            Assert.Equal("db unavailable", result.Error);
        }

        [Fact]
        public void Test_Failure_Cannot_Be_Constructed_Without_An_Error_Message()
        {
            // This invariant is what makes the guard in Value able to rely on IsFailure alone.
            Assert.Throws<InvalidOperationException>(() => Result.Fail<string>(string.Empty));
            Assert.Throws<InvalidOperationException>(() => Result.Fail(string.Empty));
        }

        [Fact]
        public void Test_Failure_From_Multiple_Messages_Joins_Them()
        {
            var result = Result.Fail<string>(["first problem", "second problem"]);

            Assert.True(result.IsFailure);
            Assert.Contains("first problem", result.Error);
            Assert.Contains("second problem", result.Error);
        }

        [Fact]
        public void Test_Successful_Result_Cannot_Carry_A_Null_Value()
        {
            Assert.Throws<ArgumentNullException>(() => Result.Ok<string>(null!));
        }
    }
}
