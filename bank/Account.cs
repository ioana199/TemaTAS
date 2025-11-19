using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace bank
{
    // Replace incorrect exception definition with proper constructors
    public class NotEnoughFundsException : ApplicationException
    {
        public NotEnoughFundsException() : base("Not enough funds in account!") { }
    }

    // Interfata pentru serviciul de conversie valutara
    // Permite injectarea de implementari diferite (reale sau stub pentru testing)
    public interface ICurrencyConverter
    {
        // Obtine cursul de schimb EUR -> RON (cati lei pentru 1 EUR)
        float GetEurToRonRate();
    }

    // Interfata pentru serviciul de notificare (pentru teste MOCK)
    // Mock Object = obiect care simuleaza comportamentul unei dependente externe
    // si permite verificarea ca metodele au fost apelate corect
    public interface INotificationService
    {
        // Trimite notificare prin email
        void SendEmail(string recipient, string subject, string message);
        
        // Trimite notificare prin SMS
        void SendSms(string phoneNumber, string message);
        
        // Logheaza o actiune in sistem
        void LogActivity(string accountId, string activity);
    }

    // Model pentru o tranzactie
    public class Transaction
    {
        public DateTime Date { get; set; }
        public string Type { get; set; } = string.Empty;      // "Deposit", "Withdraw", "Transfer"
        public float Amount { get; set; }
        public float BalanceAfter { get; set; }
        public string Description { get; set; } = string.Empty;
    }

    // Implementare reala - ar trebui sa fetch-eze cursul de la BNR API
    // In productie, aceasta ar face un HTTP request la API-ul BNR
    public class BnrCurrencyConverter : ICurrencyConverter
    {
        public float GetEurToRonRate()
        {
            // TODO: In productie, aici ar trebui sa faci un HTTP request la BNR
            // Pentru moment, returnez un curs aproximativ (4.97 RON = 1 EUR)
            // BNR API: https://www.bnr.ro/nbrfxrates.xml
            return 4.97f;
        }
    }

    public class Account
    {
        private float balance;        // sold curent (float -> atentie la precizie)
        private float minBalance = 1; // prag minim permis in cont
        private ICurrencyConverter currencyConverter; // serviciu pentru conversie valutara
        private INotificationService? notificationService; // serviciu pentru notificari (optional pentru Mock testing)
        
        // Noi functionalitati
        private string accountId;     // identificator unic al contului
        private List<Transaction> transactionHistory; // istoric tranzactii
        private float dailyWithdrawLimit = 10000; // limita de retragere zilnica
        private float totalWithdrawnToday = 0; // total retras astazi
        private DateTime lastWithdrawDate = DateTime.MinValue; // data ultimei retragerile
        private float interestRate = 0.02f; // rata dobanzii anuale (2%)

        public Account()
        {
            balance = 0;              // init sold 0
            currencyConverter = new BnrCurrencyConverter(); // foloseste implementarea reala by default
            accountId = Guid.NewGuid().ToString(); // genereaza ID unic
            transactionHistory = new List<Transaction>();
        }

        public Account(int value)
        {
            balance = value;          // init sold cu o valoare
            currencyConverter = new BnrCurrencyConverter(); // foloseste implementarea reala by default
            accountId = Guid.NewGuid().ToString(); // genereaza ID unic
            transactionHistory = new List<Transaction>();
        }

        // Constructor pentru Dependency Injection - permite injectarea unui converter custom (ex: stub pentru teste)
        public Account(int value, ICurrencyConverter converter)
        {
            balance = value;
            currencyConverter = converter;
            accountId = Guid.NewGuid().ToString();
            transactionHistory = new List<Transaction>();
        }

        // Constructor cu Notification Service (pentru teste MOCK)
        public Account(int value, ICurrencyConverter converter, INotificationService notificationService)
        {
            balance = value;
            currencyConverter = converter;
            this.notificationService = notificationService;
            accountId = Guid.NewGuid().ToString();
            transactionHistory = new List<Transaction>();
        }

        public void Deposit(float amount)
        {
            balance += amount;        // adauga suma fara validare
            
            // Adauga tranzactia in istoric
            transactionHistory.Add(new Transaction
            {
                Date = DateTime.Now,
                Type = "Deposit",
                Amount = amount,
                BalanceAfter = balance,
                Description = $"Depunere de {amount} RON"
            });

            // Trimite notificare daca serviciul este disponibil (pentru Mock testing)
            notificationService?.LogActivity(accountId, $"Deposit: {amount} RON");
            
            // Trimite email pentru depuneri mari
            if (amount > 50000)
            {
                notificationService?.SendEmail("owner@example.com", "Depunere mare", 
                    $"S-a efectuat o depunere de {amount} RON in contul {accountId}");
            }
        }

        public void Withdraw(float amount)
        {
            Withdraw(amount, true); // apeleaza versiunea cu verificare limita
        }

        // Versiune interna cu parametru pentru a controla verificarea limitei zilnice
        private void Withdraw(float amount, bool checkDailyLimit)
        {
            // Verifica limita zilnica doar daca este solicitat
            if (checkDailyLimit)
            {
                ResetDailyLimitIfNeeded();
                
                if (totalWithdrawnToday + amount > dailyWithdrawLimit)
                {
                    throw new InvalidOperationException($"Ai depasit limita zilnica de retragere ({dailyWithdrawLimit} RON)");
                }
                
                totalWithdrawnToday += amount;
                lastWithdrawDate = DateTime.Now;
            }

            balance -= amount;        // scade suma
            
            // Adauga tranzactia in istoric
            transactionHistory.Add(new Transaction
            {
                Date = DateTime.Now,
                Type = "Withdraw",
                Amount = amount,
                BalanceAfter = balance,
                Description = $"Retragere de {amount} RON"
            });

            // Trimite notificare daca serviciul este disponibil
            notificationService?.LogActivity(accountId, $"Withdraw: {amount} RON");
            
            // Trimite SMS pentru retragerile mari
            if (amount > 5000)
            {
                notificationService?.SendSms("+40712345678", 
                    $"Retragere de {amount} RON din contul {accountId}");
            }
        }

        public void TransferFunds(Account destination, float amount)
        {
            destination.Deposit(amount); // transfer simplu: +la destinatie
            Withdraw(amount, false);     // -la sursa (fara verificare limita zilnica pentru transferuri)
        }

        public Account TransferMinFunds(Account destination, float amount)
        {
            // blocheaza sume nepozitive
            if (amount <= 0)
                throw new NotEnoughFundsException();

            // permite transfer doar daca soldul ramas > prag
            if (Balance - amount > MinBalance)
            {
                destination.Deposit(amount);
                Withdraw(amount, false); // fara verificare limita zilnica pentru transferuri
            }
            else
            {
                throw new NotEnoughFundsException();
            }

            return destination;        // intoarce referinta destinatiei
        }

        // Converteste RON in EUR bazat pe cursul BNR
        // amount = suma in RON de convertit
        // returneaza: suma echivalenta in EUR
        public float ConvertRonToEur(float amountRon)
        {
            if (amountRon <= 0)
                throw new ArgumentException("Suma trebuie sa fie pozitiva");

            float eurToRonRate = currencyConverter.GetEurToRonRate();
            return amountRon / eurToRonRate; // RON / (RON per EUR) = EUR
        }

        // Converteste EUR in RON bazat pe cursul BNR
        // amount = suma in EUR de convertit
        // returneaza: suma echivalenta in RON
        public float ConvertEurToRon(float amountEur)
        {
            if (amountEur <= 0)
                throw new ArgumentException("Suma trebuie sa fie pozitiva");

            float eurToRonRate = currencyConverter.GetEurToRonRate();
            return amountEur * eurToRonRate; // EUR * (RON per EUR) = RON
        }

        // Transfer international: retrage RON din contul sursa si depune EUR in contul destinatie
        // amountRon = suma in RON de transferat din contul sursa
        public void TransferRonToEur(Account destination, float amountRon)
        {
            if (amountRon <= 0)
                throw new ArgumentException("Suma trebuie sa fie pozitiva");

            // Verifica daca contul sursa are suficienti bani
            if (Balance - amountRon <= MinBalance)
                throw new NotEnoughFundsException();

            // Converteste RON -> EUR
            float amountEur = ConvertRonToEur(amountRon);

            // Retrage RON din sursa (fara limita zilnica pentru transferuri internationale)
            Withdraw(amountRon, false);

            // Depune EUR in destinatie
            destination.Deposit(amountEur);
        }

        // Transfer international: retrage EUR din contul sursa si depune RON in contul destinatie
        // amountEur = suma in EUR de transferat din contul sursa
        public void TransferEurToRon(Account destination, float amountEur)
        {
            if (amountEur <= 0)
                throw new ArgumentException("Suma trebuie sa fie pozitiva");

            // Verifica daca contul sursa are suficienti bani
            if (Balance - amountEur <= MinBalance)
                throw new NotEnoughFundsException();

            // Converteste EUR -> RON
            float amountRon = ConvertEurToRon(amountEur);

            // Retrage EUR din sursa (fara limita zilnica pentru transferuri internationale)
            Withdraw(amountEur, false);

            // Depune RON in destinatie
            destination.Deposit(amountRon);
        }

        public float Balance
        {
            get { return balance; }    // doar citire
        }

        public float MinBalance
        {
            get { return minBalance; } // prag minim configurat in clasa
        }

        public string AccountId
        {
            get { return accountId; }
        }

        public List<Transaction> TransactionHistory
        {
            get { return new List<Transaction>(transactionHistory); } // returneaza o copie pentru protectie
        }

        public float DailyWithdrawLimit
        {
            get { return dailyWithdrawLimit; }
            set { dailyWithdrawLimit = value; }
        }

        // Metode noi functionale

        // Reseteaza contorul de retrageri zilnice daca e o zi noua
        private void ResetDailyLimitIfNeeded()
        {
            if (lastWithdrawDate.Date < DateTime.Now.Date)
            {
                totalWithdrawnToday = 0;
            }
        }

        // Calculeaza dobanda pentru soldul curent
        // period = numarul de zile pentru care se calculeaza dobanda
        public float CalculateInterest(int daysCount)
        {
            if (daysCount <= 0)
                throw new ArgumentException("Numarul de zile trebuie sa fie pozitiv");

            // Dobanda simpla: sold * rata * (zile / 365)
            float interest = balance * interestRate * (daysCount / 365.0f);
            return interest;
        }

        // Aplica dobanda calculata la sold
        public void ApplyInterest(int daysCount)
        {
            float interest = CalculateInterest(daysCount);
            balance += interest;

            transactionHistory.Add(new Transaction
            {
                Date = DateTime.Now,
                Type = "Interest",
                Amount = interest,
                BalanceAfter = balance,
                Description = $"Dobanda pentru {daysCount} zile"
            });

            // Notifica aplicarea dobanzii
            notificationService?.LogActivity(accountId, $"Interest applied: {interest} RON for {daysCount} days");
        }

        // Verifica daca soldul este suficient pentru o anumita suma
        public bool HasSufficientBalance(float amount)
        {
            return balance >= amount + minBalance;
        }

        // Obtine tranzactiile dintr-o perioada
        public List<Transaction> GetTransactionsByDateRange(DateTime startDate, DateTime endDate)
        {
            return transactionHistory
                .Where(t => t.Date >= startDate && t.Date <= endDate)
                .ToList();
        }

        // Obtine tranzactiile dupa tip
        public List<Transaction> GetTransactionsByType(string type)
        {
            return transactionHistory
                .Where(t => t.Type.Equals(type, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        // Obtine suma totala depusa
        public float GetTotalDeposits()
        {
            return transactionHistory
                .Where(t => t.Type == "Deposit")
                .Sum(t => t.Amount);
        }

        // Obtine suma totala retrasa
        public float GetTotalWithdrawals()
        {
            return transactionHistory
                .Where(t => t.Type == "Withdraw")
                .Sum(t => t.Amount);
        }

        // Genereaza un raport al contului
        public string GenerateAccountReport()
        {
            var sb = new StringBuilder();
            sb.AppendLine($"=== Raport Cont {accountId} ===");
            sb.AppendLine($"Sold curent: {balance:F2} RON");
            sb.AppendLine($"Sold minim permis: {minBalance:F2} RON");
            sb.AppendLine($"Limita retragere zilnica: {dailyWithdrawLimit:F2} RON");
            sb.AppendLine($"Total retras astazi: {totalWithdrawnToday:F2} RON");
            sb.AppendLine($"Numar tranzactii: {transactionHistory.Count}");
            sb.AppendLine($"Total depuneri: {GetTotalDeposits():F2} RON");
            sb.AppendLine($"Total retragerile: {GetTotalWithdrawals():F2} RON");
            sb.AppendLine($"Rata dobanda: {interestRate * 100:F2}%");
            
            // Trimite notificare ca s-a generat raportul
            notificationService?.LogActivity(accountId, "Account report generated");
            
            return sb.ToString();
        }
    }
}
