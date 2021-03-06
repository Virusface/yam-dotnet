﻿// -----------------------------------------------------------------------
// <copyright file="ResponseErrorHandler.cs" company="YamNet">
//   Copyright (c) 2013 YamNet contributors
// </copyright>
// -----------------------------------------------------------------------

namespace YamNet.Client
{
    using System;
    using System.Net;
    using System.Threading.Tasks;

    using RestSharp;

    using YamNet.Client.Errors;
    using YamNet.Client.Exceptions;

    /// <summary>
    /// The HTTP response error handler.
    /// </summary>
    public class ResponseErrorHandler : IResponseErrorHandler
    {
        /// <summary>
        /// The deserializer.
        /// </summary>
        private readonly IDeserializer deserializer;

        /// <summary>
        /// The exception translator.
        /// </summary>
        private readonly IErrorToExceptionTranslator translator;

        /// <summary>
        /// Initializes a new instance of the <see cref="ResponseErrorHandler" /> class.
        /// </summary>
        /// <param name="deserializer">The deserializer.</param>
        /// <param name="translator">The error to exception translator.</param>
        public ResponseErrorHandler(IDeserializer deserializer, IErrorToExceptionTranslator translator)
        {
            this.deserializer = deserializer;
            this.translator = translator;
        }

        /// <summary>
        /// Returns exceptions for known error types
        /// so they can be handled properly, if desired.
        /// </summary>
        /// <param name="response">The HTTP response.</param>
        /// <returns>The <see cref="Task{Exception}"/>.</returns>
        /// <exception cref="ServerErrorException">Server error exception.</exception>
        /// <exception cref="ConnectionFailureException">Connection failure exception.</exception>
        public async Task<Exception> HandleAsync(RestResponse response)
        {
            // Currently the server will return html in this case,
            // so there's nothing useful to do.
            if (response.StatusCode == HttpStatusCode.InternalServerError)
            {
                return new ServerErrorException();
            }

            // Yammer API rate limit exceeded
            // Reference: https://developer.yammer.com/restapi/#rest-ratelimits
            if ((int)response.StatusCode == 429)
            {
                return new RateLimitExceededException();
            }

            // No valuable information in the content.
            if (response.StatusCode == HttpStatusCode.GatewayTimeout
                || response.StatusCode == HttpStatusCode.ServiceUnavailable
                || response.StatusCode == HttpStatusCode.RequestTimeout)
            {
                return new ConnectionFailureException();
            }

            // Unauthorised, include in return the message body.
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                var msg = await response.Content.ReadAsStringAsync();
                if (msg.Contains("{"))
                {
                    msg = msg.Replace("{", string.Empty).Replace("}", string.Empty);
                }

                return new UnauthorizedException(msg, response.StatusCode);
            }

            // Forbidden, include in return the message body.
            if (response.StatusCode == HttpStatusCode.Forbidden)
            {
                var msg = await response.Content.ReadAsStringAsync();
                return new ForbiddenException(msg, response.StatusCode);
            }

            // Check message body for errors.
            var errorResponse = await response.Content.ReadAsByteArrayAsync();
            if (errorResponse == null || errorResponse.Length <= 0)
            {
                return null;
            }

            // Deserialise error in the message body, if found.
            Error err;
            try
            {
                err = this.deserializer.Deserialize<Error>(errorResponse);

                if (err == null)
                {
                    return null;
                }

                if (!err.IsValid())
                {
                    var msg = await response.Content.ReadAsStringAsync();
                    var deserializationError = new Exception(msg);

                    return deserializationError;
                }
            }
            catch (Exception ex)
            {
                return ex;
            }

            // Translate any exception found into error codes.
            var exception = this.translator.Translate(response.StatusCode, err);

            return exception;
        }
    }

    /// <summary>
    /// The ResponseErrorHandler extension.
    /// </summary>
    /// <remarks>
    /// Wholly unnecessary, but added to keep a similar signature with
    /// the .Net 4+ HttpContent methods. Basically passes the string
    /// content along in async methods.
    /// </remarks>
    internal static class ResponseErrorHandlerExtension
    {
        /// <summary>
        /// Read string asynchronously.
        /// </summary>
        /// <param name="content">The string content.</param>
        /// <returns>The <see cref="Task{string}"/>.</returns>
        public async static Task<string> ReadAsStringAsync(this string content)
        {
            // Go async mad!
            var completion = new TaskCompletionSource<string>();
            await Task.Factory.StartNew(() =>
                {
                    if (string.IsNullOrEmpty(content))
                    {
                        completion.TrySetException(new ArgumentNullException());
                    }
                    else
                    {
                        completion.TrySetResult(content);
                    }

                    return completion.Task;
                });

            return await completion.Task;
        }

        /// <summary>
        /// Read string as byte array asynchronously.
        /// </summary>
        /// <param name="content">The string content.</param>
        /// <returns>The <see cref="Task{byte[]}"/>.</returns>
        public async static Task<byte[]> ReadAsByteArrayAsync(this string content)
        {
            // Go async mad!
            var completion = new TaskCompletionSource<byte[]>();
            await Task.Factory.StartNew(() =>
                {
                    if (string.IsNullOrEmpty(content))
                    {
                        completion.TrySetException(new ArgumentNullException());
                    }
                    else
                    {
                        completion.TrySetResult(System.Text.Encoding.UTF8.GetBytes(content));
                    }

                    return completion.Task;
                });

            return await completion.Task;
        }
    }
}
