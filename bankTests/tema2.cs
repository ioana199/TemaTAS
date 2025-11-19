using NUnit.Framework;

namespace bank.Tests
{
    [TestFixture]
    [Description("Teste Tema 2: Conversii valutare cu stub")]
    public class AccountCurrencyTests
    {
        private class StubConverter : ICurrencyConverter
        {
            private float rate;
            public StubConverter(float rate) { this.rate = rate; }
            public float GetEurToRonRate() => rate;
        }

        private Account? account;
        private Account? destination;

        [Test]
        public void ConvertRonToEur_ShouldReturnCorrectValue()
        {
            var stub = new StubConverter(5.0f);
            account = new Account(1000, stub);

            float eur = account.ConvertRonToEur(250);
            Assert.That(eur, Is.EqualTo(50.0f).Within(0.01));
        }

        [Test]
        public void ConvertEurToRon_ShouldReturnCorrectValue()
        {
            var stub = new StubConverter(5.0f);
            account = new Account(1000, stub);

            float ron = account.ConvertEurToRon(50);
            Assert.That(ron, Is.EqualTo(250.0f).Within(0.01));
        }

        [Test]
        public void TransferRonToEur_ShouldConvertAndTransfer()
        {
            var stub = new StubConverter(5.0f);
            account = new Account(1000, stub);
            destination = new Account(0, stub);

            account.TransferRonToEur(destination, 500);
            Assert.That(account.Balance, Is.EqualTo(500));
            Assert.That(destination.Balance, Is.EqualTo(100).Within(0.01));
        }

        [Test]
        public void TransferEurToRon_ShouldConvertAndTransfer()
        {
            var stub = new StubConverter(5.0f);
            account = new Account(200, stub);
            destination = new Account(0, stub);

            account.TransferEurToRon(destination, 50);
            Assert.That(account.Balance, Is.EqualTo(150));
            Assert.That(destination.Balance, Is.EqualTo(250).Within(0.01));
        }

        [Test]
        public void ConvertRonToEur_NegativeAmount_ShouldThrow()
        {
            var stub = new StubConverter(5.0f);
            account = new Account(1000, stub);
            Assert.Throws<ArgumentException>(() => account.ConvertRonToEur(-100));
        }

        [Test]
        public void ConvertEurToRon_NegativeAmount_ShouldThrow()
        {
            var stub = new StubConverter(5.0f);
            account = new Account(1000, stub);
            Assert.Throws<ArgumentException>(() => account.ConvertEurToRon(-50));
        }
    }
}
