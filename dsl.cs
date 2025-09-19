using System;
using System.Collections.Generic;
using System.Xml.Linq;

public static class GtkDslTranspiler
{
    // ------------------- 1. Widget Map -------------------
    static Dictionary<string, string> WidgetMap = new Dictionary<string, string>()
    {
        { "Window", "GtkWindow" },
        { "ApplicationWindow", "GtkApplicationWindow" },
        { "Box", "GtkBox" },
        { "Grid", "GtkGrid" },
        { "Button", "GtkButton" },
        { "Entry", "GtkEntry" },
        { "Label", "GtkLabel" },
        { "CheckButton", "GtkCheckButton" },
        { "RadioButton", "GtkRadioButton" },
        { "ToggleButton", "GtkToggleButton" },
        { "Image", "GtkImage" },
        { "ProgressBar", "GtkProgressBar" },
        { "Notebook", "GtkNotebook" },
        { "Stack", "GtkStack" },
        { "FlowBox", "GtkFlowBox" },
        { "ScrolledWindow", "GtkScrolledWindow" },
        { "TextView", "GtkTextView" },
        { "ComboBoxText", "GtkComboBoxText" },
        { "Scale", "GtkScale" }
    };

    // ------------------- 2. CSS + HTML Property Aliases -------------------
    static Dictionary<string, string> PropertyAliases = new Dictionary<string, string>()
    {
        { "width", "default_width" },
        { "height", "default_height" },
        { "margin-top", "margin_top" },
        { "margin-bottom", "margin_bottom" },
        { "margin-left", "margin_start" },
        { "margin-right", "margin_end" },
        { "halign", "halign" },
        { "valign", "valign" },
        { "placeholder", "placeholder_text" },
        { "value", "text" },
        { "checked", "active" },
        { "label", "label" },
        { "src", "file" }
    };

    // ------------------- 3. Widget Properties -------------------
    static Dictionary<string, HashSet<string>> WidgetProperties = new Dictionary<string, HashSet<string>>()
    {
        { "GtkWindow", new HashSet<string>{ "title","default_width","default_height","resizable","modal","decorated" } },
        { "GtkApplicationWindow", new HashSet<string>{ "title","default_width","default_height","resizable","modal","decorated" } },
        { "GtkBox", new HashSet<string>{ "orientation","spacing","homogeneous","halign","valign","margin","margin_start","margin_end","margin_top","margin_bottom" } },
        { "GtkGrid", new HashSet<string>{ "row_spacing","column_spacing","margin","margin_top","margin_bottom","margin_start","margin_end" } },
        { "GtkButton", new HashSet<string>{ "label","image","sensitive","tooltip_text","halign","valign","margin" } },
        { "GtkEntry", new HashSet<string>{ "text","placeholder_text","editable","visibility","max_length","activates_default","halign","valign" } },
        { "GtkLabel", new HashSet<string>{ "label","halign","valign","wrap","ellipsize" } },
        { "GtkCheckButton", new HashSet<string>{ "label","active","halign","valign" } },
        { "GtkRadioButton", new HashSet<string>{ "label","active","group","halign","valign" } },
        { "GtkToggleButton", new HashSet<string>{ "label","active","halign","valign" } },
        { "GtkImage", new HashSet<string>{ "file","icon_name","pixbuf" } },
        { "GtkProgressBar", new HashSet<string>{ "fraction","text","show_text","pulse_step" } },
        { "GtkNotebook", new HashSet<string>{ "tab_pos","show_tabs","show_border" } },
        { "GtkStack", new HashSet<string>{ "visible_child","transition_type" } },
        { "GtkFlowBox", new HashSet<string>{ "selection_mode","min_children_per_line","max_children_per_line" } },
        { "GtkScrolledWindow", new HashSet<string>{ "hscrollbar_policy","vscrollbar_policy","shadow_type" } },
        { "GtkTextView", new HashSet<string>{ "editable","wrap_mode","cursor_visible" } },
        { "GtkComboBoxText", new HashSet<string>{ "active","has_entry" } },
        { "GtkScale", new HashSet<string>{ "adjustment","draw_value","digits" } }
    };

    // ------------------- 4. Widget Styles -------------------
    static Dictionary<string, HashSet<string>> WidgetStyles = new Dictionary<string, HashSet<string>>()
    {
        { "GtkWindow", new HashSet<string>{ "background-color","color","opacity","margin","padding","border","border-radius" } },
        { "GtkBox", new HashSet<string>{ "background-color","color","opacity","margin","padding","border","border-radius" } },
        { "GtkButton", new HashSet<string>{ "background-color","color","font-size","font-weight","font-style","opacity","padding","margin","border","border-radius" } },
        { "GtkLabel", new HashSet<string>{ "color","font-size","font-weight","font-style","opacity","margin","padding" } },
        { "GtkEntry", new HashSet<string>{ "color","font-size","font-weight","font-style","background-color","opacity","padding","margin" } },
        { "GtkCheckButton", new HashSet<string>{ "color","font-size","font-weight","font-style","background-color","opacity","padding","margin" } },
        { "GtkToggleButton", new HashSet<string>{ "color","font-size","font-weight","font-style","background-color","opacity","padding","margin" } }
    };

    // ------------------- 5. Widget Signals -------------------
    static Dictionary<string, HashSet<string>> WidgetSignals = new Dictionary<string, HashSet<string>>()
    {
        { "GtkWindow", new HashSet<string>{ "delete-event","destroy","focus-in-event","focus-out-event","map","unmap","show","hide" } },
        { "GtkButton", new HashSet<string>{ "clicked","pressed","released","enter","leave" } },
        { "GtkToggleButton", new HashSet<string>{ "toggled","pressed","released" } },
        { "GtkCheckButton", new HashSet<string>{ "toggled" } },
        { "GtkRadioButton", new HashSet<string>{ "toggled" } },
        { "GtkEntry", new HashSet<string>{ "activate","changed","insert-text","delete-text","focus-in-event","focus-out-event" } },
        { "GtkComboBoxText", new HashSet<string>{ "changed" } },
        { "GtkScale", new HashSet<string>{ "value-changed","change-value" } }
    };

    // ------------------- 6. Signal Name Map -------------------
    static Dictionary<string, string> SignalNameMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
    {
        { "onClick", "clicked" },
        { "onActivate", "activate" },
        { "onChanged", "changed" },
        { "onToggled", "toggled" },
        { "onPress", "pressed" },
        { "onRelease", "released" }
    };

    // ------------------- 7. Transpile Function -------------------
    public static XDocument TranspileToUiXml(XmlElement node)
    {
        if (!WidgetMap.TryGetValue(node.Name, out string gtkName))
            throw new Exception($"Unsupported widget: {node.Name}");

        var element = new XElement("object",
            new XAttribute("class", gtkName),
            new XAttribute("id", node.Name)
        );

        // Properties & Signals
        if (WidgetProperties.ContainsKey(gtkName))
        {
            foreach (var kvp in node.Properties)
            {
                if (kvp.Key.StartsWith("on") && SignalNameMap.ContainsKey(kvp.Key))
                {
                    string signalName = SignalNameMap[kvp.Key];
                    if (!WidgetSignals.ContainsKey(gtkName) || !WidgetSignals[gtkName].Contains(signalName))
                        throw new Exception($"Unsupported signal '{kvp.Key}' for widget '{gtkName}'");
                    element.Add(new XElement("signal", new XAttribute("name", signalName), new XAttribute("handler", kvp.Value)));
                }
                else
                {
                    string propName = kvp.Key;
                    if (PropertyAliases.ContainsKey(kvp.Key))
                        propName = PropertyAliases[kvp.Key];

                    if (!WidgetProperties[gtkName].Contains(propName))
                        throw new Exception($"Unsupported property '{kvp.Key}' for widget '{gtkName}'");

                    element.Add(new XElement("property", new XAttribute("name", propName), kvp.Value));
                }
            }
        }

        // Styles
        if (WidgetStyles.ContainsKey(gtkName) && node.Styles.Count > 0)
        {
            var styleElem = new XElement("style");
            foreach (var kvp in node.Styles)
            {
                if (!WidgetStyles[gtkName].Contains(kvp.Key))
                    throw new Exception($"Unsupported style '{kvp.Key}' for widget '{gtkName}'");
                styleElem.Add(new XElement("property", new XAttribute("name", kvp.Key), kvp.Value));
            }
            element.Add(styleElem);
        }

        // Children
        foreach (var child in node.Children)
        {
            element.Add(TranspileToUiXml(child).Root);
        }

        return new XDocument(element);
    }
}

// ------------------- 8. Node Representation -------------------
public class XmlElement
{
    public string Name;
    public Dictionary<string, string> Properties = new Dictionary<string, string>();
    public Dictionary<string, string> Styles = new Dictionary<string, string>();
    public List<XmlElement> Children = new List<XmlElement>();
}
