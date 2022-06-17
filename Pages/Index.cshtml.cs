using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Azure.Identity;
using Microsoft.Graph;
using Microsoft.Identity.Web;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace Auth.Pages;
public class IndexModel : PageModel
{
    private readonly ILogger<IndexModel> _logger;

    public IndexModel(ILogger<IndexModel> logger)
    {
        _logger = logger;
        var options = new SubscriptionListOptions
        {
            Customer = "cus_LtXiNPKXT2dwWE",
            Limit = 3,
        };
        var service = new SubscriptionService();
        StripeList<Stripe.Subscription> subscriptions = service.List(
        options);

        Console.WriteLine(subscriptions.Data.Count);

        if (subscriptions.Data.Count != 0) {
            Console.WriteLine("True");
        }
        else
        {
            Console.WriteLine("False");
        }

    }
}
