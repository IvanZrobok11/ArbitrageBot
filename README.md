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