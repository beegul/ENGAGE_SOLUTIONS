using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using KEYLOOP.Authorization;
using KEYLOOP.Controllers;
using KEYLOOP.Entities.Brands;
using KEYLOOP.Entities.Orders;
using KEYLOOP.Entities.Parts;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Xunit;

namespace KEYLOOP.Tests.Controllers
{
    public class PartsControllerTests
    {
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly PartsController _controller;

        public PartsControllerTests()
        {
            var mockFactory = new Mock<IHttpClientFactory>();
            _mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            var mockConfiguration = new Mock<IConfiguration>();

            var client = new HttpClient(_mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri("https://api.eu-stage.keyloop.io/sample/sample/v1/")
            };

            mockFactory.Setup(factory => factory.CreateClient(It.IsAny<string>())).Returns(client);
            
            mockConfiguration.Setup(config => config["Keyloop:BaseUrl"]).Returns("https://api.eu-stage.keyloop.io/sample/sample/v1/");
            mockConfiguration.Setup(config => config["Keyloop:ClientId"]).Returns("your_client_id");
            mockConfiguration.Setup(config => config["Keyloop:ClientSecret"]).Returns("your_client_secret");
            mockConfiguration.Setup(config => config["Keyloop:EnterpriseId"]).Returns("your_enterprise_id");
            mockConfiguration.Setup(config => config["Keyloop:StoreId"]).Returns("your_store_id");
            
            var tokenResponse = new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("{ \"access_token\": \"test_access_token\" }", Encoding.UTF8, "application/json")
            };
            
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", 
                    ItExpr.Is<HttpRequestMessage>(req 
                        => req.Method == HttpMethod.Post && req.RequestUri.AbsolutePath.EndsWith("/accesstoken")),
                    ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(tokenResponse);
            
            var keyloopApiClient = new KeyloopApiClient(mockFactory.Object, new AccessToken(mockFactory.Object, mockConfiguration.Object, NullLogger<AccessToken>.Instance));
            _controller = new PartsController(keyloopApiClient, NullLogger<PartsController>.Instance, mockConfiguration.Object);
        }

        [Fact]
        public async Task SearchParts_ReturnsOkResult_WhenPartsExist()
        {
            // Arrange
            var partResponse = CreateSamplePartResponse();
            var responseContent = new StringContent(JsonSerializer.Serialize(partResponse), Encoding.UTF8, "application/json");

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = responseContent
                });

            if (partResponse.Parts != null) 
            {
                foreach (var part in partResponse.Parts)
                {
                    MockPriceAvailability(part);
                }
            }

            // Act
            var result = await _controller.SearchParts("B1", "123");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedParts = Assert.IsType<PartResponse>(okResult.Value);

            returnedParts.Should().BeEquivalentTo(partResponse);
        }

        [Fact]
        public async Task SearchParts_ReturnsBadRequest_WhenBrandCodeIsNullOrEmpty()
        {
            // Act
            var result = await _controller.SearchParts("", "123");

            // Assert
            result.Should().BeOfType<ObjectResult>(); 
        }

        [Fact]
        public async Task SearchParts_ReturnsBadRequest_WhenPartCodeIsNullOrEmpty()
        {
            // Act
            var result = await _controller.SearchParts("B1", "");

            // Assert
            result.Should().BeOfType<ObjectResult>();
        }

        [Fact]
        public async Task SearchParts_ReturnsInternalServerError_WhenExceptionOccurs()
        {
            // Arrange
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new Exception("Simulated API error"));

            // Act
            var result = await _controller.SearchParts("B1", "123");

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            statusCodeResult.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task GetBrands_ReturnsOkResult_WhenBrandsExist()
        {
            // Arrange
            var brandResponse = CreateSampleBrandResponse();
            var responseContent = new StringContent(JsonSerializer.Serialize(brandResponse), Encoding.UTF8, "application/json");

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri.AbsolutePath.EndsWith("/brands")), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = responseContent
                });

            // Act
            var result = await _controller.GetBrands();

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedBrands = Assert.IsType<BrandResponse>(okResult.Value);

            returnedBrands.Should().BeEquivalentTo(brandResponse);
        }

        [Fact]
        public async Task GetBrands_ReturnsInternalServerError_WhenExceptionOccurs()
        {
            // Arrange
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new Exception("Simulated API error"));

            // Act
            var result = await _controller.GetBrands();

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            statusCodeResult.StatusCode.Should().Be(500);
        }

        [Fact]
        public async Task PlaceOrder_ReturnsOkResult_WhenOrderIsPlaced()
        {
            // Arrange
            var orderRequest = CreateSamplePartsOrderRequest();
            var orderResponse = CreateSamplePartsOrderResponse();
            var responseContent = new StringContent(JsonSerializer.Serialize(orderResponse), Encoding.UTF8, "application/json");

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Post && req.RequestUri.AbsolutePath.EndsWith("/parts-orders")), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = responseContent
                });

            // Act
            var result = await _controller.PlaceOrder(orderRequest);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedOrder = Assert.IsType<PartsOrderResponse>(okResult.Value);

            returnedOrder.Should().BeEquivalentTo(orderResponse);
        }

        [Fact]
        public async Task PlaceOrder_ReturnsInternalServerError_WhenExceptionOccurs()
        {
            // Arrange
            var orderRequest = CreateSamplePartsOrderRequest();

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new Exception("Simulated API error"));

            // Act
            var result = await _controller.PlaceOrder(orderRequest);

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            statusCodeResult.StatusCode.Should().Be(500);
        }
        
        [Fact]
        public async Task SearchParts_ReturnsNotFound_WhenNoPartsFound()
        {
            // Arrange
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.NotFound,
                    Content = new StringContent("No parts found.")
                });

            // Act
            var result = await _controller.SearchParts("B1", "123");

            // Assert
            result.Should().BeOfType<NotFoundObjectResult>();
        }
        
        [Fact]
        public async Task SearchParts_ReturnsInternalServerError_WhenPriceAvailabilityFails()
        {
            // Arrange
            var partResponse = CreateSamplePartResponse();
            var responseContent = new StringContent(JsonSerializer.Serialize(partResponse), Encoding.UTF8, "application/json");

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri.AbsolutePath.StartsWith("/parts")), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = responseContent
                });

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri.AbsolutePath.EndsWith("/price-availability")), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    Content = new StringContent("Price availability failed.")
                });

            // Act
            var result = await _controller.SearchParts("B1", "123");

            // Assert
            result.Should().BeOfType<ObjectResult>();
            var objectResult = (ObjectResult)result;
            objectResult.StatusCode.Should().Be(500);
        }
        
        private static PartResponse CreateSamplePartResponse()
        {
            return new PartResponse
            {
                Parts = new List<Part>
                {
                    new()
                    {
                        PartId = "partId1",
                        PartCode = 123,
                        BrandCode = "B1",
                        Description = "Part 1",
                        Price = 10.5m,
                        Availability = 5,
                        AlternativeParts = new AlternativeParts
                        {
                            SupersessionDate = DateTime.UtcNow,
                            AlternativeType = "TYPE1",
                            Parts = new List<Part>
                            {
                                new() { PartId = "altPartId1", PartCode = 124, BrandCode = "B1", Description = "Alt Part 1", Price = 12.0m, Availability = 2 },
                                new() { PartId = "altPartId2", PartCode = 125, BrandCode = "B1", Description = "Alt Part 2", Price = 15.0m, Availability = 8 }
                            }
                        }
                    },
                    new Part { PartId = "partId2", PartCode = 456, BrandCode = "B2", Description = "Part 2", Price = 20.0m, Availability = 10 }
                },
                TotalItems = 2,
                TotalPages = 1
            };
        }

        private static BrandResponse CreateSampleBrandResponse()
        {
            return new BrandResponse
            {
                Brands = new List<Brand>
                {
                    new() { BrandCode = "B1", Description = "Brand 1" },
                    new() { BrandCode = "B2", Description = "Brand 2" }
                },
                TotalItems = 2,
                TotalPages = 1,
            };
        }

        private static PartsOrderRequest CreateSamplePartsOrderRequest()
        {
            return new PartsOrderRequest
            {
                CustomerId = "customerId1",
                CompanyId = "companyId1",
                OrderContact = new OrderContact { Name = "John Doe", Phone = "1234567890", Email = "TestEmail@email.com", CompanyName = "Company A" },
                AlternateDeliveryAddress = new AlternateDeliveryAddress
                {
                    StreetName = "Delivery Street",
                    PostalCode = 54321,
                    City = "Delivery City",
                    CountryCode = "US"
                },
                OrderType = "ORDER_TYPE_1",
                OrderReference = "ORDER_REF_123",
                Parts = new List<PartOrder>
                {
                    new() { Part = new Part { PartId = "partId1" }, Quantity = 2, MandatoryVehicleReferences = new List<MandatoryVehicleReference> { new() { Type = "VIN", Value = "1234567890" } } }
                }
            };
        }

        private static PartsOrderResponse CreateSamplePartsOrderResponse()
        {
            return new PartsOrderResponse
            {
                PartsOrderId = 12345,
                PartsOrderDateTime = DateTime.UtcNow,
                OrderStatus = "CONFIRMED",
                Customer = new Customer { CustomerId = "custId1", FamilyName = "Doe", GivenName = "John", TitleCommon = "Mr." },
                Company = new Company { CompanyId = "companyId1", Reference = 123, Name = "Company A", Address = "Company Address" },
                OrderContact = new OrderContact { Name = "Contact Name" },
                DeliveryAddress = new DeliveryAddress { StreetName = "Delivery Street", PostalCode = 54321, City = "Delivery City", CountryCode = "US" },
                OrderType = "ORDER_TYPE_1",
                OrderReference = "ORDER_REF_123",
                Parts = new PartsOrder
                {
                    OrderLineId = "orderLine1",
                    PartId = "partId1",
                    Quantity = 2,
                    UnitOfMeasure = new UnitOfMeasure { Unit = "UNIT", Value = 1 },
                    UnitOfSale = 1,
                    PartsOrderLineStatus = "RESERVED",
                    MandatoryVehicleReference = new MandatoryVehicleReference { Type = "VIN", Value = "1234567890" },
                    ListPrice = new Price { NetValue = 10.5m, GrossValue = 12.6m, TaxValue = 2.1m, TaxRate = 20, CurrencyCode = "GBP" },
                    OrderPrice = new Price { NetValue = 10.0m, GrossValue = 12.0m, TaxValue = 2.0m, TaxRate = 20, CurrencyCode = "GBP" }
                }
            };
        }
        
        private void MockPriceAvailability(Part part)
        {
            var priceAvailabilityResponse = new PriceAvailability
            {
                PartId = part.PartId,
                BrandCode = part.BrandCode,
                PartCode = part.PartCode,
                ListPrice = new Price { NetValue = part.Price, GrossValue = 12.6m, TaxValue = 2.1m, TaxRate = 20, CurrencyCode = "GBP" },
                Surcharge = new Price { NetValue = 5.0m, GrossValue = 6.0m, TaxValue = 1.0m, TaxRate = 20, CurrencyCode = "GBP" },
                AvailableStock = part.Availability,
                SalesBlocked = false,
                SalesBlockedDescription = "Not blocked",
                IsAvailableForBackOrder = true,
                LeadTimeInDays = 2,
                MandatoryVehicleReferences = "VIN",
                OrderPrices = new OrderPrices { OrderPrice = new { }, OrderType = "ORDER_TYPE" }
            };
            var priceAvailabilityContent = new StringContent(JsonSerializer.Serialize(priceAvailabilityResponse), Encoding.UTF8, "application/json");

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.Is<HttpRequestMessage>(req => req.Method == HttpMethod.Get && req.RequestUri.AbsolutePath.EndsWith($"/parts/{part.PartId}/price-availability")), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = priceAvailabilityContent
                });
        }
    }
}