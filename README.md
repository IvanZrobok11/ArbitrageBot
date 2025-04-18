# ArbitrageBot

ArbitrageBot is a web application designed to find asset price differences on cryptocurrency exchanges. It provides a REST API for querying price differentials between exchanges and includes a Telegram bot integration for real-time notifications when profitable trading opportunities are discovered.

## Core Features

- **Price Difference Detection**: 
  - Monitors asset prices across multiple exchanges
  - Calculates price differentials in real-time
  - Considers network fees and withdrawal costs
  - Matches assets based on configurable percentage thresholds

- **Exchange Integration**: 
  - Binance
  - Bybit
  - KuCoin
  - MEXC
  - Gate.io
  - OKX (in development)
  - Huobi (in development)

- **Notification System**:
  - Telegram bot integration for real-time alerts
  - Customizable notification settings
  - Price difference threshold configuration
  - Asset-specific filtering options

Example Telegram Notification:
```
📊 Trading Symbol: GMRXUSDT
Price Difference: 9.29%

🟢 Buy Exchange Details:
Exchange: Mexc
Network: BSC
Price: 0.00014
Asks: 87.4%
Bids: 12.6%
Liquidity: 25.2%

🔴 Sell Exchange Details:
Exchange: KuCoin
Network: BSC
Price: 0.000153
Asks: 25.0%
Bids: 75.0%
Liquidity: 50.0%

📈 Profit Statistics:
💲 Budget: 100 USDT | Profit: 8.91 USDT
🏦 Fees: 0.376840 USDT
💲 Budget: 500 USDT | Profit: 46.05 USDT
🏦 Fees: 0.376840 USDT
💲 Budget: 1000 USDT | Profit: 92.48 USDT
🏦 Fees: 0.376840 USDT
```

- **REST API**:
  - V1 and V2 endpoints for price queries
  - Filtering capabilities
  - Network compatibility checking
  - Detailed asset pair information

## API Documentation

### Price Difference Endpoints

#### V1 API
```http
GET /api/v1/crypto/prices
```

Query Parameters:
- `minPercent`: Minimum acceptable price difference (%)
- `maxPercent`: Maximum acceptable price difference (%)
- `filterTicket`: Filter results by specific asset symbol
- `matchNetworks`: Only return pairs with matching networks (true/false)

Response includes:
- Low price asset details
- High price asset details
- Price difference percentage

#### V2 API
```http
GET /api/v2/crypto/prices
```
Enhanced version with additional data including:
- Network fees
- Withdrawal limits
- Liquidity information
- Order book depth

Example Request:
```bash
curl "http://localhost:5000/api/v2/crypto/prices?minPercent=3&maxPercent=100&matchNetworks=true&filterTicket=USDT"
```

Example Response:
```json
[
  {
    "symbol": "ARCUSDT",          // Trading pair symbol
    "quote": "USDT",             // Quote currency
    "diffPercent": 15.705,       // Price difference percentage between exchanges
    "exchangeForBuy": {
      "type": "Mexc",           // Exchange name for buying
      "price": 0.006482,        // Current price on the exchange
      "network": {
        "name": "ETH",          // Network name (e.g., ETH, BSC, TRX)
        "withdrawFee": 106,      // Fixed withdrawal fee
        "withdrawPercentageFee": -1,  // Percentage-based withdrawal fee (-1 if not applicable)
        "depositMinSize": -1,    // Minimum deposit amount (-1 if no limit)
        "withdrawMinSize": 576,  // Minimum withdrawal amount
        "withdrawMaxSize": 10000000  // Maximum withdrawal amount
      },
      "asksPercentage": 37.3,   // Percentage of ask orders in order book
      "bidsPercentage": 62.7,   // Percentage of bid orders in order book
      "liquidityPercentage": 74.6  // Overall liquidity indicator
    },
    "exchangeForSell": {
      "type": "KuCoin",         // Exchange name for selling
      // ... similar structure as exchangeForBuy ...
    },
    "stats": [
      {
        "budgetCurrency": "USDT",   // Currency used for calculations
        "budget": 100,              // Investment amount
        "fees": 4.017092,          // Total fees
        "fixedWithdrawFee": 4.017092,  // Fixed withdrawal fees
        "dynamicWithdrawForCurrencyBudgetFee": 0,  // Dynamic withdrawal fees
        "minBuyWithdrawPrice": 3.733632,   // Minimum amount for buying
        "maxBuyWithdrawPrice": 64820,      // Maximum amount for buying
        "minSellWithdrawPrice": 6.66,      // Minimum amount for selling
        "maxSellWithdrawPrice": null,      // Maximum amount for selling
        "profit": 11.687908               // Expected profit in USDT
      }
      // Additional stats for different budget amounts...
    ]
  }
]
```

The response provides comprehensive information about trading opportunities:

1. **Basic Information**:
   - `symbol`: Trading pair identifier
   - `quote`: Quote currency (e.g., USDT)
   - `diffPercent`: Price difference between exchanges

2. **Exchange Details** (for both buy and sell):
   - Current prices
   - Network information
   - Order book statistics
   - Liquidity indicators

3. **Network Information**:
   - Supported networks
   - Withdrawal fees (fixed and percentage-based)
   - Deposit and withdrawal limits

4. **Statistics for Different Budgets**:
   - Fee calculations
   - Minimum/maximum trade amounts
   - Expected profit calculations
   - Multiple budget scenarios (100, 500, 1000 USDT, etc.)

## Configuration

### Exchange API Setup
```json
"CryptoAPI": {
  "ExchangesCredentials": {
    "Binance": {
      "Key": "your-binance-api-key",
      "Secret": "your-binance-api-secret"
    }
  }
}
```

### Telegram Notifications
```json
"BotConfiguration": {
  "BotToken": "your-telegram-bot-token",
  "BotAuthPhrase": "your-auth-phrase"
}
```

### Background Service
```json
"BackgroundServices": {
  "AssetsBackgroundService": "00:00:30"  
}
```

## Project Structure

- **API Layer** (`ArbitrageBot`):
  - REST API controllers
  - Background services for price monitoring
  - Telegram bot integration

- **Business Logic** (`BusinessLogic`):
  - Exchange API integrations
  - Price comparison algorithms
  - Asset pair matching logic
  - Network fee calculations

## Setup Guide

1. Install Prerequisites:
   - .NET 9.0 SDK
   - SQL Server (for configuration storage)

2. Configuration:
   ```bash
   # Clone repository
   git clone [repository-url]
   cd ArbitrageBot

   # Configure settings
   # Edit appsettings.json with your:
   # - Exchange API credentials
   # - Telegram bot token
   # - Database connection string
   ```

3. Launch Application:
   ```bash
   dotnet restore
   dotnet run
   ```

4. API Usage:
   ```bash
   # Example API call
   curl "http://localhost:5000/api/v2/crypto/prices?minPercent=3&maxPercent=100&matchNetworks=true&filterTicket=USDT"
   ```

## Security Notes

- Secure storage of exchange API keys is critical
- Use read-only API keys when possible
- Keep Telegram bot tokens private
- Implement appropriate rate limiting
- Monitor exchange API usage limits

## Contributing

Contributions are welcome! Please:
1. Fork the repository
2. Create a feature branch
3. Submit a pull request

## License

MIT License - See LICENSE file for details.

## Disclaimer

This application is for informational purposes only. Cryptocurrency trading carries significant risks. Always perform your own research and risk assessment before trading. 