using System;
using System.Linq;
using System.Threading.Tasks;
using Telegram.Bot.Exceptions;
using Telegram.Bot.Tests.Integ.Framework;
using Telegram.Bot.Types;
using Telegram.Bot.Types.Enums;
using Xunit;

namespace Telegram.Bot.Tests.Integ.Polls
{
    [Collection(Constants.TestCollections.NativePolls)]
    [Trait(Constants.CategoryTraitName, Constants.InteractiveCategoryValue)]
    [TestCaseOrderer(Constants.TestCaseOrderer, Constants.AssemblyName)]
    public class PollMessageTests : IClassFixture<PollTestsFixture>
    {
        private ITelegramBotClient BotClient => _fixture.BotClient;

        private readonly PollTestsFixture _classFixture;
        private readonly TestsFixture _fixture;

        public PollMessageTests(TestsFixture fixture, PollTestsFixture classFixture)
        {
            _fixture = fixture;
            _classFixture = classFixture;
        }

        [OrderedFact("Should send a poll")]
        [Trait(Constants.MethodTraitName, Constants.TelegramBotApiMethods.SendPoll)]
        public async Task Should_Send_Poll()
        {
            Message message = await BotClient.SendPollAsync(
                /* chatId: */ _fixture.SupergroupChat,
                /* question: */ "Who shot first?",
                /* options: */ new[] {"Han Solo", "Greedo", "I don't care"}
            );

            Assert.Equal(MessageType.Poll, message.Type);
            Assert.NotEmpty(message.Poll.Id);
            Assert.False(message.Poll.IsClosed);

            Assert.Equal("Who shot first?", message.Poll.Question);
            Assert.Equal(3, message.Poll.Options.Length);
            Assert.Equal("Han Solo", message.Poll.Options[0].Text);
            Assert.Equal("Greedo", message.Poll.Options[1].Text);
            Assert.Equal("I don't care", message.Poll.Options[2].Text);
            Assert.All(message.Poll.Options, option => Assert.Equal(0, option.VoterCount));

            _classFixture.PollMessage = message;
        }

        [OrderedFact("Should receive a poll update")]
        public async Task Should_Receive_Poll_State_Update()
        {
            string pollId = _classFixture.PollMessage.Poll.Id;

            await _fixture.SendTestInstructionsAsync("🗳 Vote for any of the options on the poll above 👆");
            Update update = (
                await _fixture.UpdateReceiver.GetUpdatesAsync(updateTypes: UpdateType.Poll)
            ).First();

            Assert.Equal(UpdateType.Poll, update.Type);
            Assert.Equal(pollId, update.Poll.Id);
        }

        [OrderedFact("Should stop the poll")]
        [Trait(Constants.MethodTraitName, Constants.TelegramBotApiMethods.StopPoll)]
        public async Task Should_Stop_Poll()
        {
            Poll poll = await BotClient.StopPollAsync(
                /* chatId: */ _classFixture.PollMessage.Chat,
                /* messageId: */ _classFixture.PollMessage.MessageId
            );

            Assert.Equal(_classFixture.PollMessage.Poll.Id, poll.Id);
            Assert.True(poll.IsClosed);
        }

        [OrderedFact("Should throw exception due to not having enough poll options")]
        [Trait(Constants.MethodTraitName, Constants.TelegramBotApiMethods.SendPoll)]
        public async Task Should_Throw_Exception_Not_Enough_Options()
        {
            Exception exception = await Assert.ThrowsAnyAsync<Exception>(() =>
                BotClient.SendPollAsync(
                    /* chatId: */ _fixture.SupergroupChat,
                    /* question: */ "You should never see this poll",
                    /* options: */ new[] {"The only poll option"}
                )
            );

            Assert.IsType<ApiRequestException>(exception);
            Assert.Equal("Bad Request: poll must have at least 2 option", exception.Message);
        }
    }
}
