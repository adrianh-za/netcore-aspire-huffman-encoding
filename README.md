# Huffman API

A modern ASP.NET Core API for encoding and decoding text using Huffman compression. This project exposes endpoints for compressing text with Huffman coding and decoding previously compressed data. The API provides responses in an easy-to-consume format and includes metadata to ensure accurate round-trip compression and decompression.

## Features

- **Huffman Text Compression API**  
  Encode and decode text using efficient Huffman compression, with simple endpoints for both actions.

- **OpenAPI/Swagger Integration**  
  Interactive API documentation and testing via Swagger UI, available by default.

- **Status Endpoints**  
  Health and greeting endpoints for easy monitoring and integration checks.

- **.NET Aspire Support**  
  Includes distributed application orchestration and service references for .NET Aspire support.

- **Comprehensive Test Coverage**  
  Robust tests for encoding, decoding, code extraction, and error handling.

## API Overview

### Endpoints

- `POST /huffman/encode`  
  Compresses a string using Huffman coding. Returns the compressed data as a Base64-encoded string and the original length.

- `POST /huffman/decode`  
  Decompresses a Base64-encoded Huffman-encoded payload and returns the decoded string.

- `GET /status`  
  Basic status probe to check API availability.

- `GET /status/greeting`  
  Returns a greeting message with a server timestamp.

## Getting Started

### Prerequisites

- [.NET 10.0 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) with Aspire, for distributed runs

### Running the API

1. Open the solution in your preferred .Net IDE.
2. Run the `Huffman.AppHost` project or execute `bash dotnet run --project src/Huffman.Api`
3. Once the Aspire dashboard opens, click the **`./swagger/index.html`** url in the `huffman-api` service.
4. Swagger will open, play with the API.

## Testing

Unit tests for Huffman functionality are available in the test project.  
Run tests with:

1. Open the solution in your preferred .Net IDE.
2. Run the tests in the `Huffman.Tests` project or execute `bash dotnet test`
3. Once the Aspire dashboard opens, click the **`./swagger/index.html`** url in the `huffman-api` service.
4. Swagger will open, play with the API.