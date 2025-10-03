using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Windows.Forms;
using Autodesk.Connectivity.Explorer.Extensibility;
using Autodesk.Connectivity.WebServices;

namespace VaultTools.AddItemProperties;

public sealed class AddItemPropertiesExtension : IExplorerExtension
{
    private const string ItemEntityClassId = "ITEM";
    private const string DefaultPartNumberPropertyName = "Part Number (Item)";
    private const string DefaultRevisionPropertyName = "REV (Item)";

    private IApplication? _application;

    public void OnLogOn(IApplication application)
    {
        _application = application ?? throw new ArgumentNullException(nameof(application));
    }

    public void OnLogOff(IApplication application)
    {
        _application = null;
    }

    public IEnumerable<CommandSite> CommandSites()
    {
        var command = new CommandItem("VaultTools.AddItemProperties.Apply", "Добавить Part/REV")
        {
            Hint = "Добавляет свойства Part Number (Item) и REV (Item) к текущему редактируемому элементу."
        };

        command.UpdateCommandStatus += OnUpdateCommandStatus;
        command.Execute += OnExecuteCommand;

        var site = new CommandSite("VaultTools.AddItemProperties.ItemEditor", "Добавить свойства")
        {
            Location = CommandSiteLocation.ItemEditor
        };
        site.CommandItems.Add(command);

        return new[] { site };
    }

    public IEnumerable<CustomEntityHandler> CustomEntityHandlers()
    {
        return Enumerable.Empty<CustomEntityHandler>();
    }

    private void OnUpdateCommandStatus(object? sender, UpdateCommandStatusEventArgs e)
    {
        e.Enabled = TryGetEditedItem(e.Context) is not null;
    }

    private void OnExecuteCommand(object? sender, CommandItemEventArgs e)
    {
        if (_application?.Connection is null)
        {
            MessageBox.Show("Нет активного соединения с Vault.", "Добавление свойств", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var item = TryGetEditedItem(e.Context);
        if (item is null)
        {
            MessageBox.Show("Не удалось определить редактируемый элемент.", "Добавление свойств", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        try
        {
            ApplyProperties(item);
            MessageBox.Show($"Свойства '{DefaultPartNumberPropertyName}' и '{DefaultRevisionPropertyName}' обновлены для элемента '{item.ItemNum}'.",
                "Добавление свойств", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }
        catch (Exception ex)
        {
            MessageBox.Show("Ошибка при обновлении свойств: " + ex.Message, "Добавление свойств", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }

    private void ApplyProperties(Item item)
    {
        if (_application?.Connection?.WebServiceManager is null)
        {
            throw new InvalidOperationException("Нет активного соединения с WebServiceManager.");
        }

        var manager = _application.Connection.WebServiceManager;
        var propertyDefinitions = manager.PropertyService.GetPropertyDefinitionsByEntityClassId(ItemEntityClassId);
        var partNumberProperty = FindProperty(propertyDefinitions, DefaultPartNumberPropertyName);
        var revisionProperty = FindProperty(propertyDefinitions, DefaultRevisionPropertyName);

        if (partNumberProperty is null)
        {
            throw new InvalidOperationException($"Свойство '{DefaultPartNumberPropertyName}' не найдено.");
        }

        if (revisionProperty is null)
        {
            throw new InvalidOperationException($"Свойство '{DefaultRevisionPropertyName}' не найдено.");
        }

        var parameters = new PropInstParamArray
        {
            EntityId = item.MasterId,
            EntityClassId = ItemEntityClassId,
            PropInstParams = new[]
            {
                CreatePropertyParameter(partNumberProperty.Id, item.ItemNum),
                CreatePropertyParameter(revisionProperty.Id, item.RevNum)
            }
        };

        manager.PropertyService.SetProperties(ItemEntityClassId, new[] { parameters });
    }

    private static PropDef? FindProperty(IEnumerable<PropDef> propertyDefinitions, string displayName)
    {
        return propertyDefinitions.FirstOrDefault(def => string.Equals(def.DispName, displayName, StringComparison.OrdinalIgnoreCase));
    }

    private static PropInstParam CreatePropertyParameter(long propertyDefinitionId, string value)
    {
        return new PropInstParam
        {
            PropDefId = propertyDefinitionId,
            Val = new PropInstVal
            {
                Val = value
            }
        };
    }

    private Item? TryGetEditedItem(ICommandContext context)
    {
        if (context is null)
        {
            return null;
        }

        var contextType = context.GetType();
        foreach (var propertyName in new[] { "EditItem", "Item", "EditEntity", "EditObject", "CurrentItem", "CurrentEditObject" })
        {
            var property = contextType.GetProperty(propertyName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (property?.GetValue(context) is Item propertyItem)
            {
                return propertyItem;
            }
        }

        foreach (var methodName in new[] { "GetEditItem", "GetItem", "GetCurrentItem" })
        {
            var method = contextType.GetMethod(methodName, BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic, null, Type.EmptyTypes, null);
            if (method?.Invoke(context, null) is Item methodItem)
            {
                return methodItem;
            }
        }

        var selectionProperty = contextType.GetProperty("SelectionSet", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
            ?? contextType.GetProperty("CurrentSelectionSet", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

        if (selectionProperty?.GetValue(context) is System.Collections.IEnumerable selections)
        {
            foreach (var selection in selections)
            {
                if (selection is null)
                {
                    continue;
                }

                var typeIdProperty = selection.GetType().GetProperty("TypeId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
                var typeId = typeIdProperty?.GetValue(selection) as string;
                if (!string.IsNullOrEmpty(typeId) && !string.Equals(typeId, SelectionTypeId.Item, StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                var entityIdProperty = selection.GetType().GetProperty("EntityId", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)
                    ?? selection.GetType().GetProperty("Id", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);

                if (entityIdProperty?.GetValue(selection) is long entityId && _application?.Connection?.WebServiceManager is not null)
                {
                    return _application.Connection.WebServiceManager.ItemService.GetItemByItemId(entityId);
                }
            }
        }

        return null;
    }
}
