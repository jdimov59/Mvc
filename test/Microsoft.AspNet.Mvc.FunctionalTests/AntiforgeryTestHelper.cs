// Copyright (c) .NET Foundation. All rights reserved.
// Licensed under the Apache License, Version 2.0. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Xml.Linq;

namespace Microsoft.AspNet.Mvc.FunctionalTests
{
    public static class AntiforgeryTestHelper
    {
        public static string RetrieveAntiforgeryToken(string htmlContent, string actionUrl)
        {
            return RetrieveAntiforgeryTokens(
                htmlContent,
                attribute => attribute.Value.EndsWith(actionUrl, StringComparison.OrdinalIgnoreCase) ||
                    attribute.Value.EndsWith($"HtmlEncode[[{ actionUrl }]]", StringComparison.OrdinalIgnoreCase))
                .FirstOrDefault();
        }

        public static IEnumerable<string> RetrieveAntiforgeryTokens(
            string htmlContent,
            Func<XAttribute, bool> predicate = null)
        {
            predicate = predicate ?? (_ => true);
            htmlContent = "<Root>" + htmlContent + "</Root>";
            var reader = new StringReader(htmlContent);
            var htmlDocument = XDocument.Load(reader);

            foreach (var form in htmlDocument.Descendants("form"))
            {
                foreach (var attribute in form.Attributes())
                {
                    if (string.Equals(attribute.Name.LocalName, "action", StringComparison.OrdinalIgnoreCase)
                        && predicate(attribute))
                    {
                        foreach (var input in form.Descendants("input"))
                        {
                            if (input.Attribute("name") != null &&
                                input.Attribute("type") != null &&
                                input.Attribute("type").Value == "hidden" &&
                                (input.Attribute("name").Value == "__RequestVerificationToken" ||
                                 input.Attribute("name").Value == "HtmlEncode[[__RequestVerificationToken]]"))
                            {
                                yield return input.Attributes("value").First().Value;
                            }
                        }
                    }
                }
            }
        }

        public static CookieMetadata RetrieveAntiforgeryCookie(HttpResponseMessage response)
        {
            var setCookieArray = response.Headers.GetValues("Set-Cookie").ToArray();
            var cookie = setCookieArray[0].Split(';').First().Split('=');
            var cookieKey = cookie[0];
            var cookieData = cookie[1];

            return new CookieMetadata()
            {
                Key = cookieKey,
                Value = cookieData
            };
        }

        public class CookieMetadata
        {
            public string Key { get; set; }

            public string Value { get; set; }
        }
    }
}