using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Mvc;
using Stripe;
using Stripe.Checkout;
using Microsoft.Graph;
using System.Net.Http.Headers;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Azure.Identity;
using Microsoft.Identity.Web;
using Microsoft.Extensions.Options;
using System.Security.Principal;

public class StripeOptions
{
    public string option { get; set; }
}

namespace server.Controllers
{

    [Route("create-checkout-session")]
    [ApiController]
    public class CheckoutApiController : Controller
    {
        [HttpPost]
        public ActionResult Create()
        {  
            // Graph setup
            var scopes = new[] { "https://graph.microsoft.com/.default" };

            // Multi-tenant apps can use "common",
            // single-tenant apps must use the tenant ID from the Azure portal
            var tenantId = "6ea7a6fa-91ab-4e6c-9945-4c3633038292";

            // Values from app registration
            var clientId = "3dd7dbb4-4e74-4f80-a132-e69ca865ebf2";
            var clientSecret = ".RU8Q~1Wk.OmcUBOLPSoeM54hFdR_6hvhmptvcc2";

            // using Azure.Identity;
            var azureOptions = new TokenCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
            };

            // https://docs.microsoft.com/dotnet/api/azure.identity.clientsecretcredential
            var clientSecretCredential = new ClientSecretCredential(
                tenantId, clientId, clientSecret, azureOptions);

            var graphClient = new GraphServiceClient(clientSecretCredential, scopes);

            string oid = User.GetObjectId();

            // Start of stripe
            var domain = "https://localhost:7271";

            var priceOptions = new PriceListOptions
            {
                LookupKeys = new List<string> {
                    Request.Form["lookup_key"]
                }
            };
            var priceService = new PriceService();
            StripeList<Price> prices = priceService.List(priceOptions);

            var options = new SessionCreateOptions
            {
                LineItems = new List<SessionLineItemOptions>
                {
                  new SessionLineItemOptions
                  {
                    Price = prices.Data[0].Id,
                    Quantity = 1,
                  },
                },
                Mode = "subscription",
                SuccessUrl = domain,
                CancelUrl = domain,
                Customer = "cus_LtXiNPKXT2dwWE",
            };
            var service = new SessionService();
            Session session = service.Create(options);

            Response.Headers.Add("Location", session.Url);
            Console.WriteLine(session.Id);
            return new StatusCodeResult(303);
        }
    }

    [Route("create-portal-session")]
    [ApiController]
    public class PortalApiController : Controller
    {
        [HttpPost]
        public ActionResult Create()
        {
            // For demonstration purposes, we're using the Checkout session to retrieve the customer ID.
            // Typically this is stored alongside the authenticated user in your database.
            var checkoutService = new SessionService();
            var checkoutSession = checkoutService.Get(Request.Form["session_id"]);

            // This is the URL to which your customer will return after
            // they are done managing billing in the Customer Portal.
            var returnUrl = "http://localhost:7271";

            var options = new Stripe.BillingPortal.SessionCreateOptions
            {
                Customer = checkoutSession.CustomerId,
                ReturnUrl = returnUrl,
            };
            var service = new Stripe.BillingPortal.SessionService();
            var session = service.Create(options);

            Response.Headers.Add("Location", session.Url);
            return new StatusCodeResult(303);
        }
    }

    [Route("webhook")]
    [ApiController]
    public class WebhookController : Controller
    {
        [HttpPost]
        public async Task<IActionResult> Index()
        {
            var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
            // Replace this endpoint secret with your endpoint's unique secret
            // If you are testing with the CLI, find the secret by running 'stripe listen'
            // If you are using an endpoint defined with the API or dashboard, look in your webhook settings
            // at https://dashboard.stripe.com/webhooks
            const string endpointSecret = "whsec_780efc8af50bcec760321049a566a9ee9a7ae29b9f8c4b6dbea9c21fc5280e05";
            try
            {
                var stripeEvent = EventUtility.ParseEvent(json);
                var signatureHeader = Request.Headers["Stripe-Signature"];
                stripeEvent = EventUtility.ConstructEvent(json,
                        signatureHeader, endpointSecret);
                if (stripeEvent.Type == Events.CustomerSubscriptionDeleted)
                {
                    var subscription = stripeEvent.Data.Object as Stripe.Subscription;
                    Console.WriteLine("A subscription was canceled.", subscription.Id);
                    // Then define and call a method to handle the successful payment intent.
                    // handleSubscriptionCanceled(subscription);
                }
                else if (stripeEvent.Type == Events.CustomerSubscriptionUpdated)
                {
                    var subscription = stripeEvent.Data.Object as Stripe.Subscription;
                    Console.WriteLine("A subscription was updated.", subscription.Id);
                    // Then define and call a method to handle the successful payment intent.
                    // handleSubscriptionUpdated(subscription);
                }
                else if (stripeEvent.Type == Events.CustomerSubscriptionCreated)
                {
                    var subscription = stripeEvent.Data.Object as Stripe.Subscription;
                    Console.WriteLine("A subscription was created.", subscription.Id);
                    // Then define and call a method to handle the successful payment intent.
                    // handleSubscriptionUpdated(subscription);
                }
                else if (stripeEvent.Type == Events.CustomerSubscriptionTrialWillEnd)
                {
                    var subscription = stripeEvent.Data.Object as Stripe.Subscription;
                    Console.WriteLine("A subscription trial will end", subscription.Id);
                    // Then define and call a method to handle the successful payment intent.
                    // handleSubscriptionUpdated(subscription);
                }
                else
                {
                    Console.WriteLine("Unhandled event type: {0}", stripeEvent.Type);
                }
                return Ok();
            }
            catch (StripeException e)
            {
                Console.WriteLine("Error: {0}", e.Message);
                return BadRequest();
            }
        }
    }

    [Route("graph")]
    [ApiController]
    public class ChangeStripePropertyAsyn : Controller
    {
        [HttpPost]
        public async Task<IActionResult> ChangeStripePropertyAsync()
        {
            try
            {
            // The client credentials flow requires that you request the
            // /.default scope, and preconfigure your permissions on the
            // app registration in Azure. An administrator must grant consent
            // to those permissions beforehand.
            var scopes = new[] { "https://graph.microsoft.com/.default" };

            // Multi-tenant apps can use "common",
            // single-tenant apps must use the tenant ID from the Azure portal
            var tenantId = "6ea7a6fa-91ab-4e6c-9945-4c3633038292";

            // Values from app registration
            var clientId = "3dd7dbb4-4e74-4f80-a132-e69ca865ebf2";
            var clientSecret = ".RU8Q~1Wk.OmcUBOLPSoeM54hFdR_6hvhmptvcc2";

            // using Azure.Identity;
            var options = new TokenCredentialOptions
            {
                AuthorityHost = AzureAuthorityHosts.AzurePublicCloud
            };

            // https://docs.microsoft.com/dotnet/api/azure.identity.clientsecretcredential
            var clientSecretCredential = new ClientSecretCredential(
                tenantId, clientId, clientSecret, options);

            var graphClient = new GraphServiceClient(clientSecretCredential, scopes);

            string oid = User.GetObjectId();

            var customerOptions = new CustomerCreateOptions
            {
                Name = User.GetDisplayName(),
                Description = "My First Test Customer (created for API docs at https://www.stripe.com/docs/api)",
            };
            var service = new CustomerService();
            var customer = service.Create(customerOptions);

            // foreach (System.Security.Claims.Claim claim in User.Claims) {
            //     Console.WriteLine(claim);
            // }
            
            var user = new User
            {
                AdditionalData = new Dictionary<string, object>()
                {
                    {"extension_a47b21d5cfed44148b9d67a55acdf731_StripeID", customer.Id }
                }
            };

            await graphClient.Users[oid]
                .Request()
                .UpdateAsync(user);


            var user2 = await graphClient.Users[oid]
                .Request()
                .Select("extension_a47b21d5cfed44148b9d67a55acdf731_StripeID")
                .GetAsync();

            var lines = user2.AdditionalData.Select(kvp => kvp.Key + ": " + kvp.Value.ToString());

            Console.WriteLine(string.Join(Environment.NewLine, lines));
            
             return Ok();
            }
            catch (StripeException e)
            {
                Console.WriteLine("Error: {0}", e.Message);
                return BadRequest();
            }
        }

    }
}

