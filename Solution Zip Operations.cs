public class Script : ScriptBase
{
    public override async Task<HttpResponseMessage> ExecuteAsync()
    {
        try
        {
            // Determine the operation based on OperationId
            switch (this.Context.OperationId)
            {
                case "TextWordReplacer":
                    return await HandleTextWordReplacer().ConfigureAwait(false);

                case "GenerateDocxXml":
                    return await HandleGenerateDocxXml().ConfigureAwait(false);

                case "ListZipContents":
                    return await HandleListZipContents().ConfigureAwait(false);

                case "ExtractKeysFromJson":
                    return await HandleExtractKeysFromJson().ConfigureAwait(false);

                default:
                    // Handle unknown OperationId
                    var errorResponse = new HttpResponseMessage(HttpStatusCode.BadRequest);
                    errorResponse.Content = CreateJsonContent($"Unknown operation ID '{this.Context.OperationId}'");
                    return errorResponse;
            }
        }
        catch (Exception ex)
        {
            // Handle unexpected exceptions
            var errorResponse = new HttpResponseMessage(HttpStatusCode.InternalServerError);
            errorResponse.Content = new StringContent($"Error: {ex.Message}", Encoding.UTF8, "text/plain");
            return errorResponse;
        }
    }

    private async Task<HttpResponseMessage> HandleTextWordReplacer()
    {
        // Parse input JSON
        var contentAsString = await this.Context.Request.Content.ReadAsStringAsync().ConfigureAwait(false);
        var inputJson = JObject.Parse(contentAsString);

        // Extract inputs
        var replacements = inputJson["Replacements"];
        string textToParse = inputJson["TextToParse"]?.ToString() ?? string.Empty;
        bool ignoreCase = inputJson["IgnoreCase"]?.ToObject<bool>() ?? true;

        // Prepare regex options
        RegexOptions options = ignoreCase ? RegexOptions.IgnoreCase : RegexOptions.None;

        // Perform replacements
        foreach (var replacement in replacements)
        {
            string searchWord = replacement["SearchWord"]?.ToString() ?? string.Empty;
            string replacementWord = replacement["ReplacementWord"]?.ToString() ?? string.Empty;
            bool useRegex = replacement["UseRegex"]?.ToObject<bool>() ?? false;
            bool interpretEscapes = replacement["InterpretEscapes"]?.ToObject<bool>() ?? false;

            // Interpret escape sequences if requested
            if (interpretEscapes)
            {
                replacementWord = replacementWord
                    .Replace("\\t", "\t")  // Tab
                    .Replace("\\n", "\n")  // Newline
                    .Replace("\\r", "\r")  // Carriage Return
                    .Replace("\\b", "\b")  // Backspace
                    .Replace("\\f", "\f")  // Form Feed
                    .Replace("\\\\", "\\") // Backslash
                    .Replace("\\'", "'")   // Single Quote
                    .Replace("\\\"", "\""); // Double Quote

                // Optional: Handle Unicode or Hexadecimal Escapes
                replacementWord = Regex.Replace(replacementWord, @"\\u([0-9A-Fa-f]{4})", match =>
                {
                    return ((char)int.Parse(match.Groups[1].Value, System.Globalization.NumberStyles.HexNumber)).ToString();
                });

                replacementWord = Regex.Replace(replacementWord, @"\\x([0-9A-Fa-f]{2})", match =>
                {
                    return ((char)int.Parse(match.Groups[1].Value, System.Globalization.NumberStyles.HexNumber)).ToString();
                });
            }


            // Handle regex or plain text
            if (useRegex)
            {
                // Perform regex-based replacement
                textToParse = Regex.Replace(textToParse, searchWord, replacementWord, options);
            }
            else
            {
                // Escape the search word to treat it as literal text
                string escapedSearchWord = Regex.Escape(searchWord);
                textToParse = Regex.Replace(textToParse, escapedSearchWord, replacementWord, options);
            }
        }

        // Return the modified text
        var response = new HttpResponseMessage(HttpStatusCode.OK)
        {
            Content = new StringContent(textToParse, Encoding.UTF8, "text/plain")
        };
        return response;
    }

    private async Task<HttpResponseMessage> HandleGenerateDocxXml()
    {
        // Parse input JSON
        var contentAsString = await this.Context.Request.Content.ReadAsStringAsync().ConfigureAwait(false);
        var inputJson = JObject.Parse(contentAsString);

        // Namespaces
        XNamespace w = "http://schemas.openxmlformats.org/wordprocessingml/2006/main";
        XNamespace w14 = "http://schemas.microsoft.com/office/word/2010/wordml";

        // Generate unique IDs
        Random random = new Random();

        var body = new XElement(w + "body");

        // Main Title
        body.Add(new XElement(w + "p",
            new XAttribute(w14 + "paraId", $"{random.Next(10000000):X8}"),
            new XAttribute(w14 + "textId", $"{random.Next(10000000):X8}"),
            new XElement(w + "pPr",
                new XElement(w + "pStyle", new XAttribute(w + "val", "Heading1"))
            ),
            new XElement(w + "r",
                new XElement(w + "t", inputJson["h1 main title"]?.ToString() ?? "")
            )
        ));

        // Main Paragraph
        body.Add(new XElement(w + "p",
            new XAttribute(w14 + "paraId", $"{random.Next(10000000):X8}"),
            new XAttribute(w14 + "textId", $"{random.Next(10000000):X8}"),
            new XElement(w + "r",
                new XElement(w + "t", inputJson["h2 main paragraph"]?.ToString() ?? "")
            )
        ));

        // Create full document
        var document = new XDocument(
            new XElement(w + "document",
                body,
                new XElement(w + "sectPr",
                    new XElement(w + "pgSz",
                        new XAttribute(w + "w", "12240"),
                        new XAttribute(w + "h", "15840")
                    ),
                    new XElement(w + "pgMar",
                        new XAttribute(w + "top", "1440"),
                        new XAttribute(w + "right", "1440"),
                        new XAttribute(w + "bottom", "1440"),
                        new XAttribute(w + "left", "1440")
                    )
                )
            )
        );

        // Return the document.xml as a response
        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = new StringContent(document.ToString(), Encoding.UTF8, "application/xml");
        return response;
    }

    private async Task<HttpResponseMessage> HandleListZipContents()
    {
        // Parse input JSON
        var contentAsString = await this.Context.Request.Content.ReadAsStringAsync().ConfigureAwait(false);
        var inputJson = JObject.Parse(contentAsString);

        string base64ZipContent = inputJson["zipContentBytes"]?.ToString();
        string fileType = inputJson["fileType"]?.ToString() ?? "All";

        // Convert Base64 to byte array
        byte[] zipBytes = Convert.FromBase64String(base64ZipContent);

        // Prepare file list
        var fileList = new JArray();

        // Use MemoryStream to process the ZIP archive
        using (var memoryStream = new MemoryStream(zipBytes))
        using (var archive = new ZipArchive(memoryStream, ZipArchiveMode.Read))
        {
            foreach (var entry in archive.Entries)
            {
                if (string.IsNullOrEmpty(entry.Name)) continue;

                if (fileType != "All" && !entry.Name.EndsWith(fileType, StringComparison.OrdinalIgnoreCase)) continue;

                using (var entryStream = entry.Open())
                using (var ms = new MemoryStream())
                {
                    entryStream.CopyTo(ms);

                    fileList.Add(new JObject
                    {
                        ["name"] = entry.Name,
                        ["fullName"] = entry.FullName,
                        ["contentBytes"] = Convert.ToBase64String(ms.ToArray())
                    });
                }
            }
        }

        // Return the file list
        var result = new JObject
        {
            ["files"] = fileList,
            ["totalFileCount"] = fileList.Count,
            ["fileTypeFilter"] = fileType
        };

        var response = new HttpResponseMessage(HttpStatusCode.OK);
        response.Content = CreateJsonContent(result.ToString());
        return response;
    }

    private async Task<HttpResponseMessage> HandleExtractKeysFromJson()
    {
        // Parse input JSON
        var contentAsString = await this.Context.Request.Content.ReadAsStringAsync().ConfigureAwait(false);
        var inputJson = JObject.Parse(contentAsString);

        // Extract inputs
        string mainKey = inputJson["MainKey"]?.ToString() ?? string.Empty;
        var json = inputJson["Json"] as JObject;

        // Validate inputs
        if (string.IsNullOrEmpty(mainKey) || json == null)
        {
            return new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent("Invalid inputs: 'MainKey' or 'Json' is missing.", Encoding.UTF8, "text/plain")
            };
        }

        // Recursively search for the main key
        JToken foundToken = FindKeyRecursive(json, mainKey);

        if (foundToken != null && foundToken is JObject mainKeyObject)
        {
            // Extract and return the keys from the main key object
            var keys = new JArray(mainKeyObject.Properties().Select(p => p.Name));
            var response = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(keys.ToString(), Encoding.UTF8, "application/json")
            };
            return response;
        }
        else
        {
            return new HttpResponseMessage(HttpStatusCode.BadRequest)
            {
                Content = new StringContent($"MainKey '{mainKey}' not found or is not an object.", Encoding.UTF8, "text/plain")
            };
        }
    }

    // Helper function to recursively find a key in a nested JSON object
    private JToken FindKeyRecursive(JObject json, string key)
    {
        if (json.TryGetValue(key, out JToken token))
        {
            return token;
        }

        foreach (var property in json.Properties())
        {
            if (property.Value is JObject childObject)
            {
                var result = FindKeyRecursive(childObject, key);
                if (result != null)
                {
                    return result;
                }
            }
        }

        return null;
    }

}
