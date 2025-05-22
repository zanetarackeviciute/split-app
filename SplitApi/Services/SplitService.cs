using SplitApi.Models;

namespace SplitApi.Services;

// skaiciuoja skola grupeje is Transaction irasu
// Positive - kiek man skolingi
// Negative - keik as skolinga


public class SplitService
{
    // grazina zemeplapi, vadinama map c# <memberid, balance>
    public Dictionary<int, decimal> CalculateBalances(Group group)
    {
        var balances = new Dictionary<int, decimal>();  // tarpiniams rezultatams

        int count = group.Members.Count;  // kiek nariu, jei reikes splitint po lygiai


        // kiekvienai is tranzakciju
        foreach (var tx in group.Transactions)
        {

            decimal share = tx.Amount / count;   // kiek kiekvienas sumoka

            foreach (var m in group.Members) // kiekvienas narys
            {
                if (!balances.ContainsKey(m.Id)) balances[m.Id] = 0; // jei naujas

                if (m.Id == tx.PayerId)
                    balances[m.Id] += tx.Amount - share;  // jis sumokejo daugiau
                else
                    balances[m.Id] -= share;               // kiti skolingi
            }
        }

        return balances;
    }
}
