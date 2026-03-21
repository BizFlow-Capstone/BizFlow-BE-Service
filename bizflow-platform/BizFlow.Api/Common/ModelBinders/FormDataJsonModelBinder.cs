using Microsoft.AspNetCore.Mvc.ModelBinding;
using System.Text.Json;

namespace BizFlow.Api.Common.ModelBinders
{
    /// <summary>
    /// Custom model binder that deserializes JSON string from form data into a complex object
    /// </summary>
    public class FormDataJsonModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
            {
                throw new ArgumentNullException(nameof(bindingContext));
            }

            var modelName = bindingContext.ModelName;
            
            // Try to get the value from form data
            var valueProviderResult = bindingContext.ValueProvider.GetValue(modelName);

            if (valueProviderResult == ValueProviderResult.None)
            {
                // No value found, return empty list or default
                return Task.CompletedTask;
            }

            bindingContext.ModelState.SetModelValue(modelName, valueProviderResult);

            var value = valueProviderResult.FirstValue;

            // If value is null or empty, return default (empty list for List<T>)
            if (string.IsNullOrWhiteSpace(value))
            {
                return Task.CompletedTask;
            }

            try
            {
                // Deserialize JSON string to model type with UTF-8 support
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping // Support Unicode characters
                };
                
                var result = JsonSerializer.Deserialize(value, bindingContext.ModelType, options);
                
                if (result != null)
                {
                    bindingContext.Result = ModelBindingResult.Success(result);
                }
                else
                {
                    // Deserialization returned null, set empty list as default
                    bindingContext.Result = ModelBindingResult.Success(Activator.CreateInstance(bindingContext.ModelType));
                }
            }
            catch (JsonException ex)
            {
                var errorMessage = $"Cannot parse JSON array for {modelName}. Make sure it's valid format. Error: {ex.Message}";
                bindingContext.ModelState.TryAddModelError(modelName, errorMessage);
                bindingContext.Result = ModelBindingResult.Failed();
            }

            return Task.CompletedTask;
        }
    }
    
    /// <summary>
    /// Provider for FormDataJsonModelBinder
    /// </summary>
    public class FormDataJsonModelBinderProvider : IModelBinderProvider
    {
        public IModelBinder? GetBinder(ModelBinderProviderContext context)
        {
            if (context == null)
            {
                throw new ArgumentNullException(nameof(context));
            }

            // Only apply to List<T> types where T is a complex object (like PriceTier, ImportItem)
            if (context.Metadata.ModelType.IsGenericType &&
                context.Metadata.ModelType.GetGenericTypeDefinition() == typeof(List<>))
            {
                var elementType = context.Metadata.ModelType.GetGenericArguments()[0];
                
                // Exclude simple types. The default ASP.NET Core binder handles List<string> from query/form perfectly.
                if (elementType == typeof(string) || 
                    elementType.IsPrimitive || 
                    elementType == typeof(Guid) || 
                    elementType.IsEnum)
                {
                    return null;
                }

                if (context.BindingInfo.BindingSource == null || 
                    context.BindingInfo.BindingSource.Id == "Form" || 
                    context.BindingInfo.BindingSource.Id == "FormFile")
                {
                    return new FormDataJsonModelBinder();
                }
            }

            return null;
        }
    }
}
