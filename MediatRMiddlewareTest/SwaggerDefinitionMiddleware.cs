using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Dynamic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

namespace MediatRMiddlewareTest
{
    public class SwaggerDefinitionMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<SwaggerDefinitionMiddleware> _logger;

        public SwaggerDefinitionMiddleware(RequestDelegate next, ILoggerFactory loggerFactory)
        {
            _next = next;
            _logger = loggerFactory.CreateLogger<SwaggerDefinitionMiddleware>();
        }

        public async Task Invoke(HttpContext context)
        {
            if(context.Request.Path.ToString() != "/api/swagger")
            {
                await _next.Invoke(context);
                return;
            }

            var assembly = Assembly.GetExecutingAssembly();

            dynamic doc = new ExpandoObject();
            doc.swagger = "2.0";
            doc.info = new ExpandoObject();
            doc.info.title = assembly.GetName().Name;
            doc.info.version = "1.0.0";
            doc.host = "localhost:5000/";// context.Session..Request..Request..RequestUri.Authority;
            doc.basePath = "/";
            doc.schemes = new[] { "https" };
            if (doc.host.Contains("127.0.0.1") || doc.host.Contains("localhost"))
            {
                doc.schemes = new[] { "http" };
            }
            doc.definitions = new ExpandoObject();
            doc.paths = GeneratePaths(assembly, doc);
            doc.securityDefinitions = GenerateSecurityDefinitionsForApiKey();// GenerateSecurityDefinitions();

            var swaggerJson = (string)JsonConvert.SerializeObject(doc);

            context.Response.ContentType = "application/json";
            await context.Response.WriteAsync(swaggerJson).ConfigureAwait(false);
        }

        private static dynamic GenerateSecurityDefinitions()
        {
            dynamic securityDefinitions = new ExpandoObject();
            securityDefinitions.basic = new ExpandoObject();
            securityDefinitions.basic.type = "basic";
            //securityDefinitions.apikeyQuery = new ExpandoObject();
            //securityDefinitions.apikeyQuery.type = "apiKey";
            //securityDefinitions.apikeyQuery.name = "code";
            //securityDefinitions.apikeyQuery.@in = "query";

            // Microsoft Flow import doesn't like two apiKey options, so we leave one out.

            //securityDefinitions.apikeyHeader = new ExpandoObject();
            //securityDefinitions.apikeyHeader.type = "apiKey";
            //securityDefinitions.apikeyHeader.name = "x-functions-key";
            //securityDefinitions.apikeyHeader.@in = "header";
            return securityDefinitions;
        }

        private static dynamic GenerateSecurityDefinitionsForApiKey()
        {
            dynamic securityDefinitions = new ExpandoObject();
            securityDefinitions.apiKey = new ExpandoObject();
            securityDefinitions.apiKey.type = "apiKey";
            securityDefinitions.apiKey.name = "Authorization";
            securityDefinitions.apiKey.@in = "header";

            return securityDefinitions;
        }

        private static dynamic GenerateSecurityDefinitionsForBearerAuth()
        {
            dynamic securityDefinitions = new ExpandoObject();
            securityDefinitions.bearerAuth = new ExpandoObject();
            securityDefinitions.bearerAuth.type = "http";
            securityDefinitions.bearerAuth.scheme = "bearer";
            securityDefinitions.bearerAuth.bearerFormat = "JWT";
            //securityDefinitions.apikeyQuery = new ExpandoObject();
            //securityDefinitions.apikeyQuery.type = "apiKey";
            //securityDefinitions.apikeyQuery.name = "code";
            //securityDefinitions.apikeyQuery.@in = "query";

            // Microsoft Flow import doesn't like two apiKey options, so we leave one out.

            //securityDefinitions.apikeyHeader = new ExpandoObject();
            //securityDefinitions.apikeyHeader.type = "apiKey";
            //securityDefinitions.apikeyHeader.name = "x-functions-key";
            //securityDefinitions.apikeyHeader.@in = "header";
            return securityDefinitions;
        }

        private static dynamic GeneratePaths(Assembly assembly, dynamic doc)
        {
            dynamic paths = new ExpandoObject();

            var mediatrRequests = Assembly.GetAssembly(typeof(MediatRMiddleware)).GetTypes().Where(t => t.GetCustomAttribute<RouteAttribute>() != null).ToDictionary(t => t, t => t.GetCustomAttribute<RouteAttribute>());

            foreach(var requestType in mediatrRequests.Keys)
            {
                dynamic path = new ExpandoObject();

                var verb = mediatrRequests[requestType].HttpMethod.ToLower();

                dynamic operation = new ExpandoObject();
                operation.operationId = mediatrRequests[requestType].Route + ToTitleCase(verb); //ToTitleCase(functionAttr.Name) + ToTitleCase(verb);
                operation.produces = new[] { "application/json" };
                operation.consumes = new[] { "application/json" };
                operation.parameters = GenerateRequestParametersSignature(requestType, mediatrRequests[requestType].HttpMethod, mediatrRequests[requestType].Route, doc);

                operation.tags = new string[] { GetTag(requestType) };

                // Summary is title
                operation.summary = requestType.Name;// "Request name";//GetFunctionName(methodInfo, functionAttr.Name);
                // Verbose description
                operation.description = "Request description";// GetFunctionDescription(methodInfo, functionAttr.Name);

                operation.responses = GenerateResponseParameterSignature(requestType, doc);
                dynamic keyQuery = new ExpandoObject();
                keyQuery.apiKey = new string[0];
                operation.security = new ExpandoObject[] { keyQuery };

                AddToExpando(path, verb, operation);
                AddToExpando(paths, mediatrRequests[requestType].Route, path);
            }

            return paths;
        }

        private static string GetTag(Type requestType)
        {
            return requestType.Namespace;
        }

        private static string GetFunctionDescription(MethodInfo methodInfo, string funcName)
        {
            var displayAttr = (DisplayAttribute)methodInfo.GetCustomAttributes(typeof(DisplayAttribute), false)
                .SingleOrDefault();
            return !string.IsNullOrWhiteSpace(displayAttr?.Description) ? displayAttr.Description : $"This function will run {funcName}";
        }

        /// <summary>
        /// Max 80 characters in summary/title
        /// </summary>
        private static string GetFunctionName(MethodInfo methodInfo, string funcName)
        {
            var displayAttr = (DisplayAttribute)methodInfo.GetCustomAttributes(typeof(DisplayAttribute), false)
                .SingleOrDefault();
            if (!string.IsNullOrWhiteSpace(displayAttr?.Name))
            {
                return displayAttr.Name.Length > 80 ? displayAttr.Name.Substring(0, 80) : displayAttr.Name;
            }
            return $"Run {funcName}";
        }

        private static string GetPropertyDescription(PropertyInfo propertyInfo)
        {
            var displayAttr = (DisplayAttribute)propertyInfo.GetCustomAttributes(typeof(DisplayAttribute), false)
                .SingleOrDefault();
            return !string.IsNullOrWhiteSpace(displayAttr?.Description) ? displayAttr.Description : $"This returns {propertyInfo.PropertyType.Name}";
        }

        private static dynamic GenerateResponseParameterSignature(Type requestType, dynamic doc)
        {
            dynamic responses = new ExpandoObject();
            dynamic responseDef = new ExpandoObject();
            responseDef.description = "OK";

            var genericRequestType = requestType.GetInterfaces().FirstOrDefault(t => t.IsGenericType && t.GetGenericTypeDefinition() == typeof(IRequest<>));

            var returnType = genericRequestType.GetGenericArguments().First();
            
            if (returnType.IsGenericType)
            {
                var genericReturnType = returnType.GetGenericArguments().FirstOrDefault();
                if (genericReturnType != null)
                {
                    returnType = genericReturnType;
                }
            }
            
            if (returnType != typeof(void))
            {
                responseDef.schema = new ExpandoObject();

                if (returnType.Namespace == "System")
                {
                    // Warning:
                    // Allthough valid, it's always better to wrap single values in an object
                    // Returning { Value = "foo" } is better than just "foo"
                    SetParameterType(returnType, responseDef.schema, null);
                }
                else
                {
                    string name = returnType.Name;
                    if (returnType.IsGenericType)
                    {
                        var realType = returnType.GetGenericArguments()[0];
                        if (realType.Namespace == "System")
                        {
                            dynamic inlineSchema = GetObjectSchemaDefinition(null, returnType);
                            responseDef.schema = inlineSchema;
                        }
                        else
                        {
                            AddToExpando(responseDef.schema, "$ref", "#/definitions/" + name);
                            AddParameterDefinition((IDictionary<string, object>)doc.definitions, returnType);
                        }
                    }
                    else
                    {
                        AddToExpando(responseDef.schema, "$ref", "#/definitions/" + name);
                        AddParameterDefinition((IDictionary<string, object>)doc.definitions, returnType);
                    }
                }
            }
            AddToExpando(responses, "200", responseDef);
            return responses;
        }

        private static List<object> GenerateRequestParametersSignature(Type requestType, string httpMethod, string route, dynamic doc)
        {
            var parameterSignatures = new List<object>();

            dynamic opParam = new ExpandoObject();

            foreach (var parameter in requestType.GetProperties())
            {
                if (route.Contains($"{{{parameter.Name}}}") || httpMethod.Equals(HttpMethods.Get, StringComparison.InvariantCultureIgnoreCase))
                {
                    opParam = new ExpandoObject();
                    opParam.name = parameter.Name;
                    opParam.@in = route.Contains($"{{{parameter.Name}}}") ? "path" : "query";
                    opParam.required = true;
                    SetParameterType(parameter.PropertyType, opParam, null);
                    parameterSignatures.Add(opParam);
                }
            }

            if(httpMethod.Equals(HttpMethods.Get, StringComparison.InvariantCultureIgnoreCase))
            {
                return parameterSignatures;
            }

            opParam = new ExpandoObject();
            opParam.name = requestType.Name;
            opParam.@in = "body";
            opParam.required = true;
            opParam.schema = new ExpandoObject();
            
            AddToExpando(opParam.schema, "$ref", "#/definitions/" + requestType.Name);
            AddParameterDefinition((IDictionary<string, object>)doc.definitions, requestType);
            
            parameterSignatures.Add(opParam);
                
            return parameterSignatures;
        }

        private static List<object> GenerateFunctionParametersSignature(Type requestType, string route, dynamic doc)
        {
            var parameterSignatures = new List<object>();
            foreach (var parameter in requestType.GetProperties())
            {
                if (route.Contains($"{{{parameter.Name}}}"))
                {
                    dynamic opParam = new ExpandoObject();
                    opParam.name = parameter.Name;
                    opParam.@in = "path";
                    opParam.required = true;
                    SetParameterType(parameter.PropertyType, opParam, null);
                    parameterSignatures.Add(opParam);
                }
                else
                {
                    dynamic opParam = new ExpandoObject();
                    opParam.name = parameter.Name;
                    opParam.@in = "body";
                    opParam.required = true;
                    opParam.schema = new ExpandoObject();
                    if (parameter.PropertyType.Namespace == "System")
                    {
                        SetParameterType(parameter.PropertyType, opParam.schema, null);
                    }
                    else
                    {
                        AddToExpando(opParam.schema, "$ref", "#/definitions/" + parameter.PropertyType.Name);
                        AddParameterDefinition((IDictionary<string, object>)doc.definitions, parameter.PropertyType);
                    }
                    parameterSignatures.Add(opParam);
                }
            }
            return parameterSignatures;
        }

        private static void AddObjectProperties(Type t, string parentName, List<object> parameterSignatures, dynamic doc)
        {
            var publicProperties = t.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            foreach (PropertyInfo property in publicProperties)
            {
                if (!string.IsNullOrWhiteSpace(parentName))
                {
                    parentName += ".";
                }
                if (property.PropertyType.Namespace != "System")
                {
                    AddObjectProperties(property.PropertyType, parentName + property.Name, parameterSignatures, doc);
                }
                else
                {
                    dynamic opParam = new ExpandoObject();

                    opParam.name = parentName + property.Name;
                    opParam.@in = "query";
                    opParam.required = property.GetCustomAttributes().Any(attr => attr is RequiredAttribute);
                    opParam.description = GetPropertyDescription(property);
                    SetParameterType(property.PropertyType, opParam, doc.definitions);
                    parameterSignatures.Add(opParam);
                }
            }
        }

        private static void AddParameterDefinition(IDictionary<string, object> definitions, Type parameterType)
        {
            dynamic objDef;
            if (!definitions.TryGetValue(parameterType.Name, out objDef))
            {
                objDef = GetObjectSchemaDefinition(definitions, parameterType);
                definitions.Add(parameterType.Name, objDef);
            }
        }

        private static dynamic GetObjectSchemaDefinition(IDictionary<string, object> definitions, Type parameterType)
        {
            dynamic objDef = new ExpandoObject();
            objDef.type = "object";
            objDef.properties = new ExpandoObject();
            var publicProperties = parameterType.GetProperties(BindingFlags.Public | BindingFlags.Instance);
            List<string> requiredProperties = new List<string>();
            foreach (PropertyInfo property in publicProperties)
            {
                if (property.GetCustomAttributes().Any(attr => attr is RequiredAttribute))
                {
                    requiredProperties.Add(property.Name);
                }
                dynamic propDef = new ExpandoObject();
                propDef.description = GetPropertyDescription(property);
                SetParameterType(property.PropertyType, propDef, definitions);
                AddToExpando(objDef.properties, property.Name, propDef);
            }
            if (requiredProperties.Count > 0)
            {
                objDef.required = requiredProperties;
            }
            return objDef;
        }

        private static void SetParameterType(Type parameterType, dynamic opParam, dynamic definitions)
        {
            var inputType = parameterType;

            var setObject = opParam;
            if (inputType.IsArray)
            {
                opParam.type = "array";
                opParam.items = new ExpandoObject();
                setObject = opParam.items;
                parameterType = parameterType.GetElementType();
            }
            else if (inputType.IsGenericType)
            {
                opParam.type = "array";
                opParam.items = new ExpandoObject();
                setObject = opParam.items;
                parameterType = parameterType.GetGenericArguments()[0];
            }

            if (inputType.Namespace == "System" || (inputType.IsGenericType && inputType.GetGenericArguments()[0].Namespace == "System"))
            {
                switch (Type.GetTypeCode(inputType))
                {
                    case TypeCode.Int32:
                        setObject.format = "int32";
                        setObject.type = "integer";
                        break;
                    case TypeCode.Int64:
                        setObject.format = "int64";
                        setObject.type = "integer";
                        break;
                    case TypeCode.Single:
                        setObject.format = "float";
                        setObject.type = "number";
                        break;
                    case TypeCode.Double:
                        setObject.format = "double";
                        setObject.type = "number";
                        break;
                    case TypeCode.String:
                        setObject.type = "string";
                        break;
                    case TypeCode.Byte:
                        setObject.format = "byte";
                        setObject.type = "string";
                        break;
                    case TypeCode.Boolean:
                        setObject.type = "boolean";
                        break;
                    case TypeCode.DateTime:
                        setObject.format = "date";
                        setObject.type = "string";
                        break;
                    default:
                        setObject.type = "string";
                        break;
                }
            }
            else if (inputType.IsEnum)
            {
                opParam.type = "string";
                opParam.@enum = Enum.GetNames(inputType);
            }
            else if (definitions != null)
            {
                AddToExpando(setObject, "$ref", "#/definitions/" + parameterType.Name);
                AddParameterDefinition((IDictionary<string, object>)definitions, parameterType);
            }
        }

        public static string ToTitleCase(string str)
        {
            return CultureInfo.CurrentCulture.TextInfo.ToTitleCase(str);
        }

        public static void AddToExpando(ExpandoObject obj, string name, object value)
        {
            ((IDictionary<string, object>)obj).Add(name, value);
        }
    }
}
