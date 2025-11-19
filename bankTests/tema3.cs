using NUnit.Framework;
using Moq;

namespace bank.Tests
{
    [TestFixture]
    [Description("Teste Tema 3: MOCK pentru notificari")]
    public class AccountNotificationTests
    {
        private Account? account;
        private Mock<INotificationService>? mockService;

        [SetUp]
        public void Setup()
        {
            mockService = new Mock<INotificationService>();
        }

        [Test]
        public void Deposit_LargeAmount_ShouldCallSendEmail()
        {
            var stub = new Account(10000, new CurrencyConverterStub(5.0f), mockService.Object);
            stub.Deposit(60000);

            mockService.Verify(m => m.SendEmail(
                "owner@example.com",
                "Depunere mare",
                It.Is<string>(s => s.Contains("60000"))
            ), Times.Once);

            mockService.Verify(m => m.LogActivity(It.IsAny<string>(), It.Is<string>(s => s.Contains("Deposit"))), Times.Once);
        }

        [Test]
        public void Withdraw_LargeAmount_ShouldCallSendSms()
        {
            var stub = new Account(50000, new CurrencyConverterStub(5.0f), mockService.Object);
            stub.Withdraw(7000);

            mockService.Verify(m => m.SendSms("+40712345678", It.Is<string>(s => s.Contains("7000"))), Times.Once);
            mockService.Verify(m => m.LogActivity(It.IsAny<string>(), It.Is<string>(s => s.Contains("Withdraw"))), Times.Once);
        }

        [Test]
        public void GenerateAccountReport_ShouldCallLogActivity()
        {
            var stub = new Account(10000, new CurrencyConverterStub(5.0f), mockService.Object);
            stub.Deposit(5000);
            stub.Withdraw(2000);

            mockService.Reset(); // curata contorul
            string report = stub.GenerateAccountReport();

            mockService.Verify(m => m.LogActivity(It.IsAny<string>(), "Account report generated"), Times.Once);
            Assert.That(report, Does.Contain("Raport Cont"));
        }

        [Test]
        public void Deposit_SmallAmount_ShouldNotCallSendEmail()
        {
            var stub = new Account(10000, new CurrencyConverterStub(5.0f), mockService.Object);
            stub.Deposit(1000);

            mockService.Verify(m => m.SendEmail(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
            mockService.Verify(m => m.LogActivity(It.IsAny<string>(), It.Is<string>(s => s.Contains("Deposit"))), Times.Once);
        }
    }
}
