﻿using System;
using System.Collections.Generic;

namespace RestSharp.Portable
{
    /// <summary>
    /// Compares parameters by name
    /// </summary>
    public class ParameterComparer : IEqualityComparer<Parameter>, IComparer<Parameter>
    {
        private readonly StringComparer _stringComparer;
        private readonly bool _isGetRequest;

        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterComparer" /> class.
        /// </summary>
        /// <param name="client">The client this parameter comparer is for</param>
        /// <param name="request">The request this parameter comparer is for</param>
        public ParameterComparer(IRestClient client, IRestRequest request)
            : this(client, request, null)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="ParameterComparer" /> class.
        /// </summary>
        /// <param name="client">The client this parameter comparer is for</param>
        /// <param name="request">The request this parameter comparer is for</param>
        /// <param name="stringComparer">The string comparer to use (default: Ordinal)</param>
        public ParameterComparer(IRestClient client, IRestRequest request, StringComparer stringComparer)
        {
            _isGetRequest = request == null || client.GetEffectiveHttpMethod(request) != Method.POST;
            var nameComparer = stringComparer;
            if (nameComparer == null && request != null)
                nameComparer = request.ParameterNameComparer;
            if (nameComparer == null && client != null)
                nameComparer = client.DefaultParameterNameComparer;
            if (nameComparer == null)
                nameComparer = StringComparer.Ordinal;
            _stringComparer = nameComparer;
        }

        /// <summary>
        /// Parameters have the same name?
        /// </summary>
        /// <param name="x">The parameter to compare with</param>
        /// <param name="y">The parameter to compare to</param>
        /// <returns>true if both parameters are equal</returns>
        public bool Equals(Parameter x, Parameter y)
        {
            return Compare(x, y) == 0;
        }

        /// <summary>
        /// Calculate the hash code for a given parameter
        /// </summary>
        /// <param name="obj">The object to get the hash code for</param>
        /// <returns>The hash code</returns>
        public int GetHashCode(Parameter obj)
        {
            var paramType = obj.Type;
            if (paramType == ParameterType.RequestBody)
                return paramType.GetHashCode();

            var isGetParameter = obj.Type == ParameterType.QueryString || (_isGetRequest && obj.Type == ParameterType.GetOrPost);
            if (isGetParameter)
                paramType = ParameterType.QueryString;

            return obj.GetType().FullName.GetHashCode()
                ^ _stringComparer.GetHashCode(obj.Name ?? string.Empty)
                ^ paramType.GetHashCode();
        }

        /// <summary>
        /// Compare two parameters
        /// </summary>
        /// <param name="x">The parameter to compare with</param>
        /// <param name="y">The parameter to compare to</param>
        /// <returns>0 if both parameters are equal, &lt;0 when the <paramref name="x"/> is &lt; <paramref name="y"/>, otherwise &gt;0.</returns>
        public int Compare(Parameter x, Parameter y)
        {
            // Both must be parameters of the same type
            var xTypeName = x.GetType().FullName;
            var yTypeName = y.GetType().FullName;
            var result = string.Compare(xTypeName, yTypeName, StringComparison.Ordinal);
            if (result != 0)
                return result;

            // Types don't match?
            result = x.Type.CompareTo(y.Type);
            if (result != 0)
            {
                // When we have a GET request, we treat QueryString and GetOrPost as the same!
                var isGetParameterX = x.Type == ParameterType.QueryString || (_isGetRequest && x.Type == ParameterType.GetOrPost);
                var isGetParameterY = y.Type == ParameterType.QueryString || (_isGetRequest && y.Type == ParameterType.GetOrPost);
                if (isGetParameterX != isGetParameterY)
                    return result;
            }

            // When the parameter type is "RequestBody", then the name is irrelevant
            if (x.Type == ParameterType.RequestBody)
                return 0;

            var nameX = x.Name ?? string.Empty;
            var nameY = y.Name ?? string.Empty;

            result = _stringComparer.Compare(nameX, nameY);
            return result;
        }
    }
}
