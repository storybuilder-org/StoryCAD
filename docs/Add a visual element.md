1. Update Page layout to add the new control. 
   Add a corresponding property to the Page's ViewModel. 
   Add a 2-way binding from the Page control's Text or SelectedItem to the ViewModel    property.
   Initialize the property in the ViewModel's constructor.
   If the control is a ComboBox or other control that uses an ItemsSource,  you
   also need to add a 1-way binding from the page to that list in the ViewModel,
   and to provide a source for the list in the ViewModel. The source will usually
   be a list in Controls.ini, which is in the \Assets\Install folder. Use an existing
   control as an example. Note that the list must be in the form of key/value pairs.
   Test this much and very the layout looks okay. Insure that it's responsive 
   by resizing the page up and down and checking the layout.
2. Add the corresponding property to the Model. Name it identically to the
   ViewModel's property.
   Initialize the property in each of the Model's three constructors. 
   Update the ViewModel's LoadModel method to assign the ViewModel's property
   from the Model when the ViewModel is activated (navigated to- see BindablePage).
   If the property is a RichEditBox, call StoryReader.GetRtfText instead using a
   simple assignment statement (see other rtf fields for an example.)
   Update the ViewModel's SaveModel method to assign the Model's property from
   the ViewModel when the ViewModel is deactivated (navigated from.) If the 
   property is a RichEditBox, call StoryWriter.PutRtfText instead of a simple assignment.
   Test that changes to the field persist when you navigate from one StoryElement to
   another in the TreeView.
3. Add code to StoryReader to read the Model property from the .stbx file:
   Update the appropriate StoryElement's parse method (called from RecurseStoryElement).
   These methods are case statements to find the property's named attribute in the xml
   node and move its inner text to the Model's property.
4. Add code to StoryWriter to write the Model property to the .stbx file.
   The appropriate method will named 'ParseXElement', ex., ParseSettingElement. 
   Use an existing property as a template.
   Create a new XmlAttribute.
   If the property is a RichEditBox, you must next set the Model's property by calling
   PutRtfText.
   Assign the attribute with the property's value.
   Add the XmlAttribute to the current XmlNode.
   Test by using the new property, saving the story outline, re-opening the story project,    and verifying that the data entry from the new control is present and correct.

TODO:
Tools
ItemsSource and Controls.ini (or Tools.ini)
Editing and ComboBox (which I still need to straighten out)
 