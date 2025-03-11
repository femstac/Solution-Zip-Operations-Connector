# Solution-Zip-Operations

This Custom Connector is built for **Power Automate** to simplify common operations with JSON objects, text manipulation, document creation, and ZIP file handling. 

## Features

### 1. **Extract Keys from JSON**
- Extracts a list of keys from a specified main key within nested JSON objects.

### 2. **Text Word Replacer**
- Replace words or patterns within text.
- Supports regular expressions.
- Optionally interprets escape sequences (e.g., `\t`, `\n`).
- Allows case-insensitive replacements.

### 3. **Generate DOCX XML**
- Creates `document.xml` for `.docx` files dynamically based on JSON input.
- Useful for programmatic document generation and templating.

### 4. **List ZIP Contents**
- Lists and extracts file contents from ZIP archives.
- Supports filtering by file type (e.g., `.docx`, `.xlsx`).

## How to Use

### Installation
1. Create a custom connector
1. copy the provided YAML file into the swagger editor of the Custom Connector.
2. Copy the provided C# code into the code section of Custom connector. Be sure to enable code.
3. Publish the connector and start using it in your workflows.

### Operations Overview

- **ExtractKeysFromJson**
  - Input: `MainKey` (string), `Json` (object)
  - Output: Array of key names.

- **TextWordReplacer**
  - Input:
    - `TextToParse` (string)
    - `Replacements`: Array of `{SearchWord, ReplacementWord, UseRegex, InterpretEscapes}`
    - `IgnoreCase` (boolean)
  - Output: Modified text.

- **GenerateDocxXml**
  - Input: Document structure details (`h1MainTitle`, `h2MainParagraph`, `repeatingSection`, etc.)
  - Output: XML content for `.docx` file generation.

- **ListZipContents**
  - Input: Base64-encoded ZIP file content, optional file type filter
  - Output: List of files with Base64-encoded contents and metadata.


## Contributing
Feel free to fork this repository, create branches, and submit pull requests. Contributions to improve performance, extend functionality, or enhance usability are welcome.

## License
This project is open source and available under the MIT License.

---

**Created by:** Oluwafemi Ajigbayi

**Last updated:** March 2024

