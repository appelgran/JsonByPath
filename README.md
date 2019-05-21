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

### Example 1 - Simply getting a value

    var firstName = order.GetString("billing_address.given_name", "");
    var totalAmountIncTax = order.GetInt("order_amount", 0);

### Example 2 - Iterating array

    var orderItemNames = order.GetArray("order_lines").Select(x => JsonByPath.Use(x).GetString("name", ""));
    
### Example 3 - Dig deep

    var availableAttributes = order.GetArray("order_lines[0].available_attributes");
