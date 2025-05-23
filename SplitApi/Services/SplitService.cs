using SplitApi.Models;

namespace SplitApi.Services;
using SplitApi;

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

    // skaicuoja balansa itraukiant nauja mokejima
    public Dictionary<int, decimal> CalculateBalances(Group group, SplitDto dto)
    {
        // visos senos tx po lygiai
        var balances = CalculateBalances(group);
        int count = group.Members.Count;

        // naujas mokejimas pasirinktu budu
        switch (dto.Mode)
        {
            case SplitMode.Equally:
            {
                decimal share = dto.Amount / count;
                foreach (var m in group.Members)
                {
                    if (m.Id == dto.PayerId)
                        balances[m.Id] += dto.Amount - share;
                    else
                        balances[m.Id] = share;
                }
                break;
            }

            case SplitMode.ByPercent:
            {
                if (dto.Percentages == null || dto.Percentages.Keys.Except(group.Members.Select(m => m.Id)).Any() || Math.Abs(dto.Percentages.Values.Sum() - 100m) > 0.01m)
                {
                    throw new ArgumentException("Invalid Percentages for this group");
                }
                foreach (var m in group.Members)
                {
                    decimal pct = dto.Percentages[m.Id] / 100m;
                    decimal share = dto.Amount * pct;
                    if (m.Id == dto.PayerId)
                        balances[m.Id] += dto.Amount - share;
                    else
                        balances[m.Id] -= share;
                }
                break;
            }
            
            case SplitMode.Custom:
            {
                if (dto.Shares == null || dto.Shares.Keys.Except(group.Members.Select(m => m.Id)).Any() || dto.Shares.Values.Sum() != dto.Amount)
                {
                    throw new ArgumentException("Invalid Shares for this group");
                }
                foreach (var m in group.Members)
                {
                    decimal share = dto.Shares[m.Id];
                    if (m.Id == dto.PayerId)
                        balances[m.Id] += dto.Amount - share;
                    else
                        balances[m.Id] -= share;
                }
                break;
            }

            default:
                throw new ArgumentOutOfRangeException();
        }
        return balances;
    }
}

