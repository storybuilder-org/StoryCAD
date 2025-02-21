﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Windows.Storage;
using StoryCAD.Models;
using StoryCAD.Services.Outline;
using Microsoft.SemanticKernel.ChatCompletion;
using Microsoft.SemanticKernel;

namespace StoryCAD.Services.API
{
    /// <summary>
    /// A generic response class to standardize API call responses.
    /// </summary>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public T Payload { get; set; }
    }

    public class StoryCadSemanticKernelApi
    {
        private readonly OutlineService _outlineService;

        public StoryCadSemanticKernelApi(OutlineService outlineService)
        {
            _outlineService = outlineService;
        }

        /// <summary>
        /// Creates a new empty story outline based on a template.
        /// Parameters:
        ///   filePath - full path to the file that will back the outline
        ///   name - the name to use for the outline's Overview element
        ///   author - the author name for the overview
        ///   templateIndex - index (as a string) specifying the template to use
        /// Returns a JSON-serialized ApiResponse payload containing a list of the StoryElement Guids.
        /// </summary>
        [KernelFunction, Description("Creates a new empty story outline from a template.")]
        public async Task<ApiResponse<List<Guid>>> CreateEmptyOutline(string filePath, string name, string author, string templateIndex)
        {
            var response = new ApiResponse<List<Guid>>();
            if (!int.TryParse(templateIndex, out int idx))
            {
                response.Success = false;
                response.ErrorMessage = $"'{templateIndex}' is not a valid template index.";
                return response;
            }

            try
            {
                // Get the StorageFile from the provided file path.
                StorageFile file = await StorageFile.GetFileFromPathAsync(filePath);

                // Create a new StoryModel using the OutlineService.
                StoryModel model = await _outlineService.CreateModel(file, name, author, idx);

                // Option 1: Return the entire StoryModel as JSON:
                // response.Payload = JsonSerializer.Serialize(model);

                // Option 2: Return just a list of the StoryElement Guids.
                List<Guid> elementGuids = model.StoryElements.Select(e => e.Uuid).ToList();

                response.Success = true;
                response.Payload = elementGuids;
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.ErrorMessage = $"Error in CreateEmptyOutline: {ex.Message}";
            }

            return response;
        }

        /// <summary>
        /// Writes an updated story outline to disk.
        /// Parameters:
        ///   jsonModel - a JSON string representing the StoryModel to be saved
        ///   filePath - full path to the file where the model should be saved
        /// Returns an ApiResponse indicating success or error.
        /// </summary>
        [KernelFunction, Description("Writes the story outline to the backing store.")]
        public async Task<ApiResponse<string>> WriteOutline(string jsonModel, string filePath)
        {
            var response = new ApiResponse<string>();

            try
            {
                // Deserialize the JSON into a StoryModel object.
                StoryModel model = JsonSerializer.Deserialize<StoryModel>(jsonModel);
                if (model == null)
                {
                    response.Success = false;
                    response.ErrorMessage = "Deserialized StoryModel is null.";
                    return response;
                }


                // Write the model to disk using the OutlineService.
                await _outlineService.WriteModel(model, filePath);

                response.Success = true;
                response.Payload = "Outline written successfully.";
            }
            catch (Exception ex)
            {
                response.Success = false;
                response.ErrorMessage = $"Error in WriteOutline: {ex.Message}";
            }

            return response;
        }
    }
}
