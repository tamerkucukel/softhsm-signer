# SoftHSM-API

## Overview
The `SoftHSM-API` project provides a set of API endpoints for signing transactions and retrieving blockchain addresses using the SoftHSM service. This project is built using ASP.NET Core and uses the NBitcoin and Nethereum libraries for handling Bitcoin and Ethereum transactions.

## Features
- **Sign Bitcoin Transactions:** Sign a given Bitcoin transaction with the specified key path and coins.
- **Sign Ethereum Transactions:** Sign a given Ethereum transaction with the specified key path and transaction input.
- **Get Bitcoin Address:** Retrieve the Bitcoin address for a given key path.
- **Get Ethereum Address:** Retrieve the Ethereum address for a given key path.

## Prerequisites
- [Docker](https://www.docker.com/)
- [Docker Compose](https://docs.docker.com/compose/)

## Getting Started

### Clone the Repository
```sh
git clone https://github.com/tamerkucukel/softhsm-signer.git
cd softhsm-signer
```

### Using Docker Compose

#### Build and Run the Services

Make sure Docker and Docker Compose are installed and running on your machine.

```sh
docker compose build
docker compose up -d
```

This will build the Docker images and start the services defined in the `docker-compose.yml` file. The API will be available at `http://localhost:8090` by default.

## API Endpoints

### Sign Bitcoin Transaction

**Endpoint:** `POST /api/softhsm/bitcoin/{network}/transaction/sign`

**Request:**
```json
{
  "keyPath": "string",       // Example: "m/44'/0'/0'/0/0" (string)
  "from": "string",          // Example: "1A1zP1eP5QGefi2DMPTfTL5SLmv7DivfNa" (Bitcoin address as string)
  "to": "string",            // Example: "1BoatSLRHtKNngkdXEeobR76b53LETtpyT" (Bitcoin address as string)
  "amount": 0.001,           // Example: 0.001 (decimal, BTC unit)
  "coins": ["string"],       // Example: ["coin1SerializedData", "coin2SerializedData"] (array of strings)
  "feeRate": 0.0001          // Example: 0.0001 (decimal, Satoshis per byte)
}

```

To generate requests `Bitcoin.UnsignedTransaction` can be used in SoftHSM_API_NET_8.Models.
- `keyPath` should be in form that specified in [BIP44](https://github.com/bitcoin/bips/blob/master/bip-0044.mediawiki).
- `coins` can be obtained using Serializer class in NBitcoin library by converting NBitcoin.Coin objects to string with `ToString<T>(T obj, Network network)` method. Network should be always provided when serializing.
- `network` is set in BitcoinAdapter, otherwise you should provide network as string in lowercase.

**Response:**

- **200 OK:** Returns the signed raw transaction hex as a string.
- **400 Bad Request:** Returns an error message if the JSON is invalid or if required fields are missing.

### Get Bitcoin Address

**Endpoint:** `GET /api/softhsm/bitcoin/{network}/address/{type}/{keyPath}`

- `keyPath` should be in form that specified in [BIP44](https://github.com/bitcoin/bips/blob/master/bip-0044.mediawiki).
- `network` is set in BitcoinAdapter, otherwise you should provide network as string in lowercase.
- `type` 0 for Legacy, 1 for SegWit and 3 for TaprootBIP86 addresses.

**Response:**

- **200 OK:** Returns the bitcoin adress generated from keypath according its network and type setting.
- **400 Bad Request:** Returns an error message if the keypath is invalid or if required fields are missing or invalid.

### Sign Ethereum Transaction

**Endpoint:** `POST /api/softhsm/ethereum/transaction/sign/{keyPath}`

**Request:**
```json
{
  "from": "string",             // Example: "0x1234567890abcdef1234567890abcdef12345678" (Ethereum address as string)
  "to": "string",               // Example: "0xabcdef1234567890abcdef1234567890abcdef12" (Ethereum address as string)
  "gas": "string",              // Example: "21000" (string representing gas limit)
  "gasPrice": "string",         // Example: "20000000000" (string representing gas price in Wei)
  "value": "string",            // Example: "1000000000000000000" (string representing value in Wei, 1 ETH in this case)
  "data": "string",             // Example: "0xabcdef" (string representing input data in hexadecimal)
  "nonce": "string",            // Example: "0" (string representing transaction nonce)
  "type": "string",             // Example: "0x2" (string representing transaction type, e.g., 0x2 for EIP-1559)
  "maxPriorityFeePerGas": "string", // Example: "2000000000" (string representing max priority fee in Wei for EIP-1559)
  "maxFeePerGas": "string",     // Example: "21000000000" (string representing max total fee in Wei for EIP-1559)
}

```
Requst format is serialized version of `TransactionInput` which can be found in Nethereum library.
- `keyPath` should be in form that specified in [BIP44](https://github.com/bitcoin/bips/blob/master/bip-0044.mediawiki).

**Response:**

- **200 OK:** Returns the signed raw transaction hex as a string.
- **400 Bad Request:** Returns an error message if the JSON is invalid or if required fields are missing.

### Get Ethereum Address

**Endpoint:** `GET /api/softhsm/ethereum/address/{keyPath}`

**Parameters:**

- `keyPath`: The key path for which the Ethereum address is to be retrieved.
	- `keyPath` should be in form that specified in [BIP44](https://github.com/bitcoin/bips/blob/master/bip-0044.mediawiki).
		- Example: `m/44'/0'/0'/0/0` (unescaped representation)
	- `keyPath` should be in its escaped representation.

**Response:**

- **200 OK:** Returns the Ethereum address as a string.
- **400 Bad Request:** Returns an error message if the `keyPath` is not provided or if there is any other issue.

## Extras

On production change `ASPNETCORE_ENVIRONMENT` from Development to Production in `docker-compose.yml`.
