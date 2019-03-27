﻿//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated by a tool.
//     Runtime Version:4.0.30319.42000
//
//     Changes to this file may cause incorrect behavior and will be lost if
//     the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace Microsoft.Azure.Cosmos {
    using System;
    using System.Reflection;


    /// <summary>
    ///   A strongly-typed resource class, for looking up localized strings, etc.
    /// </summary>
    // This class was auto-generated by the StronglyTypedResourceBuilder
    // class via a tool like ResGen or Visual Studio.
    // To add or remove a member, edit your .ResX file then rerun ResGen
    // with the /str option, or rebuild your VS project.
    [global::System.CodeDom.Compiler.GeneratedCodeAttribute("System.Resources.Tools.StronglyTypedResourceBuilder", "4.0.0.0")]
    [global::System.Diagnostics.DebuggerNonUserCodeAttribute()]
    [global::System.Runtime.CompilerServices.CompilerGeneratedAttribute()]
    internal class ClientResources {
        
        private static global::System.Resources.ResourceManager resourceMan;
        
        private static global::System.Globalization.CultureInfo resourceCulture;
        
        [global::System.Diagnostics.CodeAnalysis.SuppressMessageAttribute("Microsoft.Performance", "CA1811:AvoidUncalledPrivateCode")]
        internal ClientResources() {
        }
        
        /// <summary>
        ///   Returns the cached ResourceManager instance used by this class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Resources.ResourceManager ResourceManager {
            get {
                if (object.ReferenceEquals(resourceMan, null)) {
                    global::System.Resources.ResourceManager temp = new global::System.Resources.ResourceManager("Microsoft.Azure.Cosmos.ClientResources", typeof(ClientResources).GetTypeInfo().Assembly);
                    
                    resourceMan = temp;
                }
                return resourceMan;
            }
        }
        
        /// <summary>
        ///   Overrides the current thread's CurrentUICulture property for all
        ///   resource lookups using this strongly typed resource class.
        /// </summary>
        [global::System.ComponentModel.EditorBrowsableAttribute(global::System.ComponentModel.EditorBrowsableState.Advanced)]
        internal static global::System.Globalization.CultureInfo Culture {
            get {
                return resourceCulture;
            }
            set {
                resourceCulture = value;
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The client does not have any valid token for the requested resource {0}..
        /// </summary>
        internal static string AuthTokenNotFound {
            get {
                return ResourceManager.GetString("AuthTokenNotFound", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Query expression is invalid, expression {0} is unsupported. Supported expressions are &apos;Queryable.Where&apos;, &apos;Queryable.Select&apos; &amp; &apos;Queryable.SelectMany&apos;.
        /// </summary>
        internal static string BadQuery_InvalidExpression {
            get {
                return ResourceManager.GetString("BadQuery_InvalidExpression", resourceCulture);
            }
        }

        /// <summary>
        ///   Looks up a localized string similar to Session object retrieved from client with endpoint {0} cannot be used on a client initialized to endpoint {1}..
        /// </summary>
        internal static string BadSession {
            get {
                return ResourceManager.GetString("BadSession", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Binary operator &apos;{0}&apos; is not supported..
        /// </summary>
        internal static string BinaryOperatorNotSupported {
            get {
                return ResourceManager.GetString("BinaryOperatorNotSupported", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Constructor invocation is not supported..
        /// </summary>
        internal static string ConstructorInvocationNotSupported {
            get {
                return ResourceManager.GetString("ConstructorInvocationNotSupported", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Expected a static IQueryable or IEnumerable extension method, received an instance method..
        /// </summary>
        internal static string ExpectedMethodCallsMethods {
            get {
                return ResourceManager.GetString("ExpectedMethodCallsMethods", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Expression with NodeType &apos;{0}&apos; is not supported..
        /// </summary>
        internal static string ExpressionTypeIsNotSupported {
            get {
                return ResourceManager.GetString("ExpressionTypeIsNotSupported", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Expression tree cannot be processed because evaluation of Spatial expression failed..
        /// </summary>
        internal static string FailedToEvaluateSpatialExpression {
            get {
                return ResourceManager.GetString("FailedToEvaluateSpatialExpression", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Input is not of type IDocumentQuery..
        /// </summary>
        internal static string InputIsNotIDocumentQuery {
            get {
                return ResourceManager.GetString("InputIsNotIDocumentQuery", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Incorrect number of arguments for method &apos;{0}&apos;. Expected &apos;{1}&apos; but received &apos;{2}&apos;..
        /// </summary>
        internal static string InvalidArgumentsCount {
            get {
                return ResourceManager.GetString("InvalidArgumentsCount", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to This method should only be called within Linq expression to Invoke a User-defined function..
        /// </summary>
        internal static string InvalidCallToUserDefinedFunctionProvider {
            get {
                return ResourceManager.GetString("InvalidCallToUserDefinedFunctionProvider", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Range low value must be less than or equal the high value..
        /// </summary>
        internal static string InvalidRangeError {
            get {
                return ResourceManager.GetString("InvalidRangeError", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The count value provided for a Take expression must be an integer..
        /// </summary>
        internal static string InvalidTakeValue {
            get {
                return ResourceManager.GetString("InvalidTakeValue", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to MediaLink is invalid.
        /// </summary>
        internal static string MediaLinkInvalid {
            get {
                return ResourceManager.GetString("MediaLinkInvalid", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Member binding is not supported..
        /// </summary>
        internal static string MemberBindingNotSupported {
            get {
                return ResourceManager.GetString("MemberBindingNotSupported", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Method &apos;{0}&apos; is not supported..
        /// </summary>
        internal static string MethodNotSupported {
            get {
                return ResourceManager.GetString("MethodNotSupported", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Not supported..
        /// </summary>
        internal static string NotSupported {
            get {
                return ResourceManager.GetString("NotSupported", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Method &apos;{0}&apos; is not supported. Only LINQ Methods are supported..
        /// </summary>
        internal static string OnlyLINQMethodsAreSupported {
            get {
                return ResourceManager.GetString("OnlyLINQMethodsAreSupported", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Only path expressions are supported for SelectMany..
        /// </summary>
        internal static string PathExpressionsOnly {
            get {
                return ResourceManager.GetString("PathExpressionsOnly", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to The right hand side of string.CompareTo() comparison must be constant &apos;0&apos;.
        /// </summary>
        internal static string StringCompareToInvalidConstant {
            get {
                return ResourceManager.GetString("StringCompareToInvalidConstant", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Invalid operator for string.CompareTo(). Vaid operators are (&apos;==&apos;, &apos;&lt;&apos;, &apos;&lt;=&apos;, &apos;&gt;&apos; or &apos;&gt;=&apos;).
        /// </summary>
        internal static string StringCompareToInvalidOperator {
            get {
                return ResourceManager.GetString("StringCompareToInvalidOperator", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to User-defined function name can not be null or empty..
        /// </summary>
        internal static string UdfNameIsNullOrEmpty {
            get {
                return ResourceManager.GetString("UdfNameIsNullOrEmpty", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unary operator &apos;{0}&apos; is not supported..
        /// </summary>
        internal static string UnaryOperatorNotSupported {
            get {
                return ResourceManager.GetString("UnaryOperatorNotSupported", resourceCulture);
            }
        }
        
        /// <summary>
        ///   Looks up a localized string similar to Unexpected token type: {0}.
        /// </summary>
        internal static string UnexpectedTokenType {
            get {
                return ResourceManager.GetString("UnexpectedTokenType", resourceCulture);
            }
        }
    }
}
