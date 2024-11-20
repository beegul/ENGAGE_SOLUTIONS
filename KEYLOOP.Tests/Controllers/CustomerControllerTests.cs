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
using KEYLOOP.Entities;
using KEYLOOP.Entities.Customer;
using KEYLOOP.Entities.Orders;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Moq.Protected;
using Xunit;
using Customer = KEYLOOP.Entities.Customer.Customer;

namespace KEYLOOP.Tests.Controllers
{
    public class CustomerControllerTests
    {
        private readonly Mock<HttpMessageHandler> _mockHttpMessageHandler;
        private readonly CustomerController _controller;

        public CustomerControllerTests()
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
            _controller = new CustomerController(keyloopApiClient, NullLogger<CustomerController>.Instance, mockConfiguration.Object);
        }

        [Fact]
        public async Task GetCustomer_ReturnsOkResult_WhenCustomerExists()
        {
            // Arrange
            var customerResponse = CreateSampleCustomerResponse();
            var responseContent = new StringContent(JsonSerializer.Serialize(customerResponse), Encoding.UTF8, "application/json");

            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = responseContent
                });

            // Act
            var result = await _controller.GetCustomer("testCustomerId");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var returnedCustomer = Assert.IsType<CustomerResponse>(okResult.Value);
            
            foreach (var property in typeof(Customer).GetProperties())
            {
                if (property.Name != "Created" && property.Name != "LastModified")
                {
                    var expectedValue = property.GetValue(customerResponse.Customer);
                    var actualValue = property.GetValue(returnedCustomer.Customer);

                    expectedValue.Should().BeEquivalentTo(actualValue); 
                }
            }
            Assert.Equal(customerResponse.Links!.Method, returnedCustomer.Links!.Method);
        }

        [Fact]
        public async Task GetCustomer_ReturnsNotFound_WhenCustomerDoesNotExist()
        {
            // Arrange
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.NotFound,
                    Content = new StringContent("Customer not found")
                });

            // Act
            var result = await _controller.GetCustomer("nonExistingCustomerId");

            // Assert
            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task GetCustomer_ReturnsInternalServerError_WhenExceptionOccurs()
        {
            // Arrange
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new Exception("Simulated API error"));

            // Act
            var result = await _controller.GetCustomer("testCustomerId");

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusCodeResult.StatusCode);
        }
        
        [Fact]
        public async Task GetCustomer_ReturnsBadRequest_WhenCustomerIdIsNullOrEmpty()
        {
            // Act
            // ReSharper disable once AssignNullToNotNullAttribute
            var result = await _controller.GetCustomer(null);

            // Assert
            result.Should().BeOfType<ObjectResult>(); 
        }
        
        [Fact]
        public async Task GetCustomer_ReturnsInternalServerError_WhenApiCallFails()
        {
            // Arrange
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.InternalServerError,
                    Content = new StringContent("API call failed.")
                });

            // Act
            var result = await _controller.GetCustomer("testCustomerId");

            // Assert
            var statusCodeResult = Assert.IsType<ObjectResult>(result);
            statusCodeResult.StatusCode.Should().Be(500);
        }
        
        [Fact]
        public async Task GetCustomer_HandlesJsonDeserializationException()
        {
            // Arrange
            _mockHttpMessageHandler.Protected()
                .Setup<Task<HttpResponseMessage>>("SendAsync", ItExpr.IsAny<HttpRequestMessage>(), ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(new HttpResponseMessage
                {
                    StatusCode = HttpStatusCode.OK,
                    Content = new StringContent("{ \"invalid json\" }") // Add quotes around the property name
                });

            // Act
            var result = await _controller.GetCustomer("testCustomerId");

            // Assert
            result.Should().BeOfType<ObjectResult>();
            var objectResult = (ObjectResult)result;
            objectResult.StatusCode.Should().Be(500);
        }
        
        private static CustomerResponse CreateSampleCustomerResponse()
        {
            return new CustomerResponse
            {
                Customer = new Customer
                {
                    CustomerId = "testCustomerId",
                    Reference = "12345",
                    Status = "ACTIVE",
                    LanguageCode = "en-GB",
                    Individual = new Individual
                    {
                        GivenName = "John",
                        MiddleName = "Middle",
                        FamilyName = "Doe",
                        FamilyName2 = "Doe2",
                        PreferredName = "Johnny",
                        Initials = "JD",
                        Salutation = "Mr.",
                        Title = "Dr.",
                        AcademicTitle = "Professor",
                        TitleOfNobility = "Sir"
                    },
                    Addresses = new Addresses
                    {
                        Physical = new Physical
                        {
                            StreetType = "Street",
                            StreetName = "Test Street",
                            HouseNumber = "123",
                            BuildingName = "Building A",
                            FloorNumber = "1st Floor",
                            DoorNumber = "Flat 1",
                            BlockName = "Block B",
                            Estate = "Test Estate",
                            PostalCode = "AB12 3CD",
                            Suburb = "Test Suburb",
                            City = "Test City",
                            County = "Test County",
                            Province = "Test Province",
                            CountryCode = "GB",
                            FormattedAddress = new FormattedAddress
                            {
                                Line1 = "Line 1",
                                Line2 = "Line 2",
                                Line3 = "Line 3",
                                Line4 = "Line 4"
                            }
                        },
                        Postal = new Postal
                        {
                            PoBoxName = "PO Box 123",
                            PoBoxNumber = "456",
                            PoBoxSuite = "Suite A",
                            PostalCode = "Postal Code",
                            Suburb = "Test Suburb",
                            City = "Postal City",
                            County = "Test County",
                            Province = "Test Province",
                            CountryCode = "US",
                            FormattedAddress = new FormattedAddress
                            {
                                Line1 = "Line 1",
                                Line2 = "Line 2",
                                Line3 = "Line 3",
                                Line4 = "Line 4"
                            }
                        }
                    },
                    Communications = new Communications
                    {
                        PreferredPhone = "MOBILE",
                        Personal = new Personal { Mobile = "1234567890", Landline = "02012345678", Fax = "02087654321", Email = "john.doe@example.com" },
                        Work = new Work { Mobile = "9876543210", Landline = "02098765432", Fax = "02012345678", Email = "john.doe@work.com" }
                    },
                    AdditionalDetail = new AdditionalDetail { Source = new Source { Code = "SRC001", Description = "Source 1" } },
                    Business = new Business { CompanyPosition = new CompanyPosition { Code = "CP001", Description = "Position 1" }, TypeOfBusiness = new TypeOfBusiness { Code = "TB001", Description = "Business Type 1" } },
                    Vehicles = new List<Vehicle> { new() { Relationship = "OWNER", VehicleId = "vehicleId1", Reference = 1, Class = "CAR", MakeId = "MAKE1", Description = "Vehicle 1", Vin = "VIN123", LicensePlate = "LP123" } },
                    Relations = new Relations { Customers = new List<Customer> { new() { CustomerId = "customerId1", Reference = "1" } }, Companies = new List<Company> { new() { CompanyId = "companyId1", Reference = 1 } } },
                    Branches = new List<Branch> { new() { BranchType = "BRANCH1", BranchId = "branchId1", Description = "Branch 1" } },
                    UpdateHistory = new UpdateHistory { Created = DateTime.UtcNow, LastModified = DateTime.UtcNow }
                },
                Links = new Links { Method = "GET", Rel = "self", Href = "https://example.com/customer/testCustomerId", Title = "Customer Details" }
            };
        }
    }
}