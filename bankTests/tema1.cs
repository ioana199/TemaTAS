using NUnit.Framework;
using System;

namespace bank.Tests
{
    [TestFixture]
    [Description("Teste Tema 1: Operatii cont bancar de baza")]
    public class AccountBasicTests
    {
        private Account? account;
        private Account? destination;

        [SetUp]
        public void Setup()
        {
            account = null;
            destination = null;
        }

        [TearDown]
        public void Teardown()
        {
            account = null;
            destination = null;
        }

        [Test]
        [Description("Constructor cu sold initial")]
        public void Constructor_ShouldInitializeBalanceCorrectly()
        {
            account = new Account(1000);
            Assert.That(account.Balance, Is.EqualTo(1000));
        }

        [Test]
        [Description("Deposit simplu creste soldul")]
        public void Deposit_ShouldIncreaseBalance()
        {
            account = new Account(500);
            account.Deposit(200);
            Assert.That(account.Balance, Is.EqualTo(700));
        }

        [Test]
        [Description("Withdraw simplu scade soldul")]
        public void Withdraw_ShouldDecreaseBalance()
        {
            account = new Account(500);
            account.Withdraw(300);
            Assert.That(account.Balance, Is.EqualTo(200));
        }

        [Test]
        [Description("TransferFunds actualizeaza corect ambele conturi")]
        public void TransferFunds_ShouldMoveMoneyCorrectly()
        {
            account = new Account(1000);
            destination = new Account(200);
            account.TransferFunds(destination, 400);
            Assert.That(account.Balance, Is.EqualTo(600));
            Assert.That(destination.Balance, Is.EqualTo(600));
        }

        [Test]
        [Description("TransferMinFunds valideaza domeniul si sold minim")]
        public void TransferMinFunds_ShouldRespectMinBalance()
        {
            account = new Account(500);
            destination = new Account(100);

            // Transfer valid
            account.TransferMinFunds(destination, 400);
            Assert.That(account.Balance, Is.GreaterThan(1));
            Assert.That(destination.Balance, Is.EqualTo(500));

            // Transfer invalid
            Assert.Throws<NotEnoughFundsException>(() => account.TransferMinFunds(destination, 499));
        }

        [Test]
        [Description("HasSufficientBalance returneaza corect")]
        public void HasSufficientBalance_ShouldReturnTrueOrFalse()
        {
            account = new Account(100);
            Assert.That(account.HasSufficientBalance(50), Is.True);
            Assert.That(account.HasSufficientBalance(100), Is.False);
        }
    }
}
