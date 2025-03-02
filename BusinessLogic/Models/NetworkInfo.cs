namespace BusinessLogic.Models;

public record NetworkInfo(string Name, decimal WithdrawFee, decimal? WithdrawPercentageFee, decimal? DepositMinSize, decimal? WithdrawMinSize, decimal? WithdrawMaxSize)
{
    public decimal GetWithdrawFullFee(decimal price)
    {
        if (WithdrawFee == 0 || WithdrawFee == -1)
        {
            return WithdrawFee;
        }
        return price * WithdrawFee;
    }
}
