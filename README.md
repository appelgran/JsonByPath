# JsonByPath

## How to use

    var order = new JsonByPath(jsonString);

Example JSON:

    {
        "billing_address": {
            "given_name": "Jane"
        },
        "order_amount": 5000,
        "order_lines": [
            {
                "name": "Magz",
                "available_attributes": {
                    ["duration", "extras"]
                }
            }
        ]
    }
    
### Example 1 - Dig deep

    var firstProductSecondAttribute = order.GetArray("order_lines[0].available_attributes[1]");
    // "extras"

### Example 2 - Simply getting a value

    var firstName = order.GetString("billing_address.given_name", "");
    // "Jane"
    
    var lastName = order.GetString("billing_address.family_name", "unknown");
    // "unknown" (fallback)
    
    var totalAmountIncTax = order.GetInt("order_amount", 0);
    // 5000

### Example 3 - Iterating array

    var orderItemNames = order.GetArray("order_lines").Select(x => JsonByPath.Use(x).GetString("name", ""));
    // IEnumerable<string> { "Magz" }
