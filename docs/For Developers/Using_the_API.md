---
title: Using the StoryCAD API
layout: default
nav_enabled: true
nav_order: 114
parent: For Developers
has_toc: false
---

## StoryCAD API User Manual

#### Introduction

In this manual, we explain the rationale, design, and usage of the StoryCAD API—a powerful interface that combines human and AI interactions for generating and managing comprehensive story outlines. The API harnesses the StoryCADLib and Semantic Kernel to enable users to process full prose texts and generate detailed story outlines in a single pass.

#### Rationale

The StoryCAD API was designed with two key objectives in mind:

1. **Human Interaction:**  
   The API is built to allow users to provide high-level guidance and context when creating outlines from prose. By accepting human input—whether via a console, a WinUI 3 desktop app, or other interfaces—users can steer the outline creation process, refine details, and make corrections as necessary.

2. **AI-Driven Automation:**  
   Integrating with the Semantic Kernel, the API automates much of the process by reading the full story in a single pass and generating outlines based on complex instructions. The AI uses detailed guidelines (provided either through a dedicated prompt or as part of an injected system message) to create and update story elements such as Characters, Settings, Scenes, and Problems. This dual approach enables a flexible and efficient workflow that benefits from both precise human control and the scale of automated processing.

#### API Overview

The API exposes several core functions, including:

- **CreateEmptyOutline(string name, string author, string templateIndex):**  
  Creates a new empty outline based on a provided template.
  
- **WriteOutline(string filePath):**  
  Persists the generated story outline (the StoryModel) to disk.

- **UpdateElementProperty(Guid elementUuid, string propertyName, object value):**  
  Updates a single property on a given StoryElement. This is used internally by the generic UpdateProperties routine.

- **UpdateElementProperties(Guid elementUuid, Dictionary<string, object> properties):**  
  A wrapper that iterates over a set of property updates for a StoryElement.

- **AddElement(StoryItemType typeToAdd, string parentGUID):**  
  Creates a new StoryElement under a specified parent element (usually the StoryOverview).

- **AddCastMember(Guid scene, Guid character):**
  Adds a character as part of a scene's cast.

- **GetElementsByType(StoryItemType elementType):**
  Returns all story elements of a specific type (Character, Scene, Setting, Problem, etc.).

- **Additional Functions:**
  The API also includes functions for deleting elements, retrieving element details, and creating relationships between characters.

The API is designed for both direct user interaction (through desktop or console apps) and for AI-powered processing where detailed human-like guidelines are embedded into prompts for automated execution.

#### Example Calls

###### C## Example

Below is an example of how you might use the API in a C## console application:

```csharp
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using StoryCAD.Services.API;

namespace StoryCADConsoleSample
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Create a new instance of the StoryCADApi (assuming dependency injection or manual instantiation)
            var api = new StoryCADApi();
            
            // Create an empty outline
            var outlineResult = await api.CreateEmptyOutline("The Great Adventure", "Jane Doe", "0");
            if (!outlineResult.IsSuccess)
            {
                Console.WriteLine("Failed to create outline: " + outlineResult.ErrorMessage);
                return;
            }
            Console.WriteLine("Outline created successfully.");
            
            // Suppose later we update a story element's property:
            Guid someElementGuid = outlineResult.Payload[0]; // Get a GUID from the created outline
            var updateResult = api.UpdateElementProperty(someElementGuid, "Name", "New Outline Name");
            if (!updateResult.IsSuccess)
            {
                Console.WriteLine("Failed to update element: " + updateResult.ErrorMessage);
            }
            else
            {
                Console.WriteLine("Element updated successfully.");
            }
            
            // Write the outline to disk
            var writeResult = await api.WriteOutline("C:\\Outlines\\MyStoryOutline.stbx");
            if (!writeResult.IsSuccess)
            {
                Console.WriteLine("Failed to write outline: " + writeResult.ErrorMessage);
            }
            else
            {
                Console.WriteLine("Outline written successfully.");
            }
        }
    }
}