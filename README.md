# SoftHSM-API

## Overview
The `SoftHSM-API` project provides a set of API endpoints for signing transactions and retrieving public keys using the SoftHSM service. This project is built using ASP.NET Core and uses the NBitcoin library for handling Bitcoin transactions.

## Features
- **Sign Transactions:** Sign a given transaction with the specified key path and coins.
- **Get Public Key:** Retrieve the public key for a given key path.

## Prerequisites
- [Docker](https://www.docker.com/)
- [Docker Compose](https://docs.docker.com/compose/)

## Getting Started

### Clone the Repository
```sh
git clone https://github.com/tamerkucukel/mb_softhsm-docker-api.git
cd mb_softhsm-docker-api
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

### Sign Transaction

**Endpoint:** `POST /api/softhsm/transaction/sign`

**Request:**
```json
{ 
	"txHash": "string",
	"keyPath": "string",
	"coins": ["string"],
	"network": "string"
}
```

To generate requests `UnsignedTransaction` can be used in SoftHSM_API_NET_8.Models.
- `txHash` can be obtained using `toHex()` method in Transaction class in NBitcoin library.
- `keyPath` should be in form that specified in [BIP44]([bips/bip-0044.mediawiki at master · bitcoin/bips (github.com)](https://github.com/bitcoin/bips/blob/master/bip-0044.mediawiki)).
- `coins` can be obtained using Serializer class in NBitcoin library by converting NBitcoin.Coin objects to string with `ToString<T>(T obj, Network network)` method. Network should be always provided when serializing.
- `network` can be obtained using `ToString()` method in Network class in NBitcoin library.

**Response:**

- **200 OK:** Returns the signed raw transaction hex as a string.
- **400 Bad Request:** Returns an error message if the JSON is invalid or if required fields are missing.
### Get Public Key

**Endpoint:** `GET /api/softhsm/key/{keyPath}`

**Parameters:**

- `keyPath`: The key path for which the public key is to be retrieved.
	- `keyPath` should be in form that specified in [BIP44]([bips/bip-0044.mediawiki at master · bitcoin/bips (github.com)](https://github.com/bitcoin/bips/blob/master/bip-0044.mediawiki)) .
		- Example: `m/44'/0'/0'/0/0` (unescaped representation)
	- `keyPath` should be in its escaped representation.

**Response:**

- **200 OK:** Returns the public key as a string.
- **400 Bad Request:** Returns an error message if the `keyPath` is not provided or if there is any other issue.

## Extras

On production change `ASPNETCORE_ENVIRONMENT` from Development to Production in `docker-compose.yml`.