using System;
using System.Net;
using System.Threading.Tasks;
using Client;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using System.Net.Http;
using Moq.Protected;
using System.Threading;
using System.Data.SqlClient;


namespace Adressevasker.UnitTests
{
    [TestClass]
    public class CircuitBreakerTests
    {
        [TestMethod]
        public void Should_OpenCircuit_When_FailureThresholdExceeded()
        {
            // Arrange
            var circuitBreaker = new CircuitBreaker(3, TimeSpan.FromSeconds(1), 3);

            // Act
            circuitBreaker.RegisterFailure();
            circuitBreaker.RegisterFailure();
            circuitBreaker.RegisterFailure();

            // Assert
            Assert.IsTrue(circuitBreaker.IsOpen());
        }

        [TestMethod]
        public void Should_ResetCircuit_AfterTimeoutAndRetryLimitNotReached()
        {
            // Arrange
            var circuitBreaker = new CircuitBreaker(3, TimeSpan.FromSeconds(1), 3);

            // Act
            circuitBreaker.RegisterFailure();
            circuitBreaker.RegisterFailure();
            circuitBreaker.RegisterFailure();

            // Assert
            Assert.IsTrue(circuitBreaker.IsOpen());

            // Act
            System.Threading.Thread.Sleep(5500);

            // Assert
            Assert.IsFalse(circuitBreaker.IsOpen());
        }


        [TestMethod]
        public void Should_PermanentlyOpenCircuit_AfterRetryLimitExceeded()
        {
            // Arrange
            var circuitBreaker = new CircuitBreaker(3, TimeSpan.FromSeconds(1), 1);

            // Act
            circuitBreaker.RegisterFailure();
            circuitBreaker.RegisterFailure();
            circuitBreaker.RegisterFailure();
            System.Threading.Thread.Sleep(2000);

            // Assert
            Assert.IsFalse(circuitBreaker.IsOpen());

            // Act
            circuitBreaker.RegisterFailure();
            circuitBreaker.RegisterFailure();
            circuitBreaker.RegisterFailure();

            // Assert
            Assert.IsTrue(circuitBreaker.IsOpen());
            Assert.IsFalse(circuitBreaker.CanRetry());
        }
    }


    [TestClass]
    public class ApiServiceTests
    {
        [TestMethod]
        public void Should_ReturnErrorResult_When_ApiCallFails()
        {
            // Arrange
            var mockHttpMessageHandler = new Mock<HttpMessageHandler>();
            mockHttpMessageHandler
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                    "SendAsync",
                    ItExpr.IsAny<HttpRequestMessage>(),
                    ItExpr.IsAny<CancellationToken>())
                .ThrowsAsync(new HttpRequestException("API failure"));

            var httpClient = new HttpClient(mockHttpMessageHandler.Object)
            {
                BaseAddress = new Uri("https://api.dataforsyningen.dk/")
            };

            var circuitBreaker = new CircuitBreaker(3, TimeSpan.FromSeconds(10), 3);
            var apiService = new ApiService(httpClient, circuitBreaker);

            // Act
            var result = apiService.GetAdresseVask("query=test");

            // Assert
            Assert.IsNotNull(result);
            Assert.AreEqual("An unexpected error occurred in GetAdresseVask", result.kommentar);
        }
    }

    [TestClass]
    public class LoggerTests
    {
        [TestMethod]
        public void Should_LogException_AddLogToQueue()
        {
            // Arrange
            string testContext = "TestContext";
            var testException = new Exception("Test exception message");

            // Act
            Logger.LogException(testException, testContext);

            // Assert
            string log = Logger.GetLog();
            StringAssert.Contains(log, "TestContext");
            StringAssert.Contains(log, "Test exception message");
        }

        [TestMethod]
        public void Should_GetLog_ReturnAllLogsInOrder()
        {
            // Arrange
            string context1 = "First context";
            string context2 = "Second context";

            Logger.LogException(new Exception("First exception"), context1);
            Logger.LogException(new Exception("Second exception"), context2);

            // Act
            string log = Logger.GetLog();

            // Assert
            StringAssert.Contains(log, "First context");
            StringAssert.Contains(log, "Second context");
            Assert.IsTrue(log.IndexOf("First context") < log.IndexOf("Second context"),
                "Logs are not in the correct order.");
            Assert.IsFalse(log.IndexOf("First context") > log.IndexOf("Second context"),
                "Logs are not in the correct order.");
        }
    }
}