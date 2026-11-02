// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
using System.Collections.Generic;
using System.Text.Json.Nodes;

namespace AdaptiveCards.ObjectModel.Uno
{
    public sealed class AdaptiveTableColumnDefinition
    {
        private uint? _width;
        private uint? _pixelWidth;

        public AdaptiveTableColumnDefinition()
        {
        }

        public HAlignment? HorizontalCellContentAlignment { get; set; }

        public VerticalContentAlignment? VerticalCellContentAlignment { get; set; }

        /// <summary>Relative width. Setting it clears <see cref="PixelWidth"/>.</summary>
        public uint? Width
        {
            get => _width;
            set
            {
                _width = value;
                if (value.HasValue)
                {
                    _pixelWidth = null;
                }
            }
        }

        /// <summary>Explicit pixel width. Setting it clears <see cref="Width"/>.</summary>
        public uint? PixelWidth
        {
            get => _pixelWidth;
            set
            {
                _pixelWidth = value;
                if (value.HasValue)
                {
                    _width = null;
                }
            }
        }

        internal JsonObject ToJson()
        {
            var root = new JsonObject();
            if (HorizontalCellContentAlignment.HasValue)
            {
                root["horizontalCellContentAlignment"] = EnumStrings.HAlignment.ToString(HorizontalCellContentAlignment.Value);
            }

            if (VerticalCellContentAlignment.HasValue)
            {
                root["verticalCellContentAlignment"] = EnumStrings.VerticalContentAlignment.ToString(VerticalCellContentAlignment.Value);
            }

            if (_width.HasValue)
            {
                root["width"] = _width.Value;
            }

            if (_pixelWidth.HasValue)
            {
                root["width"] = _pixelWidth.Value + "px";
            }

            return root;
        }

        internal static AdaptiveTableColumnDefinition Deserialize(ParseContext context, JsonNode jsonNode)
        {
            var json = ParseUtil.ThrowIfNotJsonObject(jsonNode);
            var definition = new AdaptiveTableColumnDefinition
            {
                HorizontalCellContentAlignment = ParseUtil.GetOptionalEnumValue(json, "horizontalCellContentAlignment", EnumStrings.HAlignment),
                VerticalCellContentAlignment = ParseUtil.GetOptionalEnumValue(
                    json, "verticalCellContentAlignment", EnumStrings.VerticalContentAlignment),
            };

            var widthValue = ParseUtil.ExtractJsonValue(json, "width");
            if (!ParseUtil.IsEmpty(widthValue))
            {
                if (ParseUtil.TryGetDouble(widthValue, out var d) && d == System.Math.Floor(d) && d >= int.MinValue && d <= int.MaxValue)
                {
                    definition.Width = (uint)(int)d;
                }
                else if (ParseUtil.IsString(widthValue))
                {
                    var pixelWidth = ParseUtil.ParseSizeForPixelSize(widthValue.GetValue<string>(), context);
                    if (pixelWidth.HasValue)
                    {
                        definition.PixelWidth = (uint)pixelWidth.Value;
                    }
                    else
                    {
                        context.AddWarning(WarningStatusCode.InvalidDimensionSpecified, "Supplied value for \"width\" could not be parsed");
                    }
                }
                else
                {
                    context.AddWarning(WarningStatusCode.InvalidValue, "Invalid type for \"width\"");
                }
            }

            return definition;
        }
    }

    public sealed class AdaptiveTableCell : AdaptiveContainer
    {
        public AdaptiveTableCell() : base(ElementType.TableCell, "TableCell")
        {
        }

        internal static AdaptiveTableCell DeserializeTableCell(ParseContext context, JsonNode jsonNode)
        {
            var json = ParseUtil.ThrowIfNotJsonObject(jsonNode);
            var idProperty = ParseUtil.GetString(json, "id");
            context.PushElement(idProperty, InternalId.Next());
            var cell = new AdaptiveTableCell();
            cell.DeserializeStyledCollection(context, json);
            cell.Rtl = ParseUtil.GetOptionalBool(json, "rtl");
            context.PopElement();
            return cell;
        }
    }

    public sealed class AdaptiveTableRow : AdaptiveCardElementBase
    {
        public AdaptiveTableRow() : base(ElementType.TableRow, "TableRow")
        {
        }

        public HAlignment? HorizontalCellContentAlignment { get; set; }

        public VerticalContentAlignment? VerticalCellContentAlignment { get; set; }

        public ContainerStyle Style { get; set; } = ContainerStyle.None;

        public IList<AdaptiveTableCell> Cells { get; } = new List<AdaptiveTableCell>();

        /// <summary>Right-to-left override for the row (not part of the WinUI3 projection).</summary>
        public bool? Rtl { get; set; }

        protected override void PopulateKnownProperties(ISet<string> knownProperties)
        {
            base.PopulateKnownProperties(knownProperties);
            knownProperties.Add("cells");
            knownProperties.Add("horizontalCellContentAlignment");
            knownProperties.Add("rtl");
            knownProperties.Add("style");
            knownProperties.Add("verticalCellContentAlignment");
        }

        public override JsonObject ToJson()
        {
            var root = base.ToJson();
            if (Cells.Count > 0)
            {
                var cells = new JsonArray();
                foreach (var cell in Cells)
                {
                    cells.Add((JsonNode)cell.ToJson());
                }

                root["cells"] = cells;
            }

            if (HorizontalCellContentAlignment.HasValue)
            {
                root["horizontalCellContentAlignment"] = EnumStrings.HAlignment.ToString(HorizontalCellContentAlignment.Value);
            }

            if (VerticalCellContentAlignment.HasValue)
            {
                root["verticalCellContentAlignment"] = EnumStrings.VerticalContentAlignment.ToString(VerticalCellContentAlignment.Value);
            }

            if (Style != ContainerStyle.None)
            {
                root["style"] = EnumStrings.ContainerStyle.ToString(Style);
            }

            if (Rtl.HasValue)
            {
                root["rtl"] = Rtl.Value;
            }

            return root;
        }

        internal override void GetResourceInformation(List<AdaptiveRemoteResourceInformation> resourceInformation)
        {
            foreach (var cell in Cells)
            {
                cell.GetResourceInformation(resourceInformation);
            }
        }

        internal static AdaptiveTableRow DeserializeTableRow(ParseContext context, JsonNode jsonNode)
        {
            var json = ParseUtil.ThrowIfNotJsonObject(jsonNode);
            var idProperty = ParseUtil.GetString(json, "id");
            context.PushElement(idProperty, InternalId.Next());

            var row = new AdaptiveTableRow();
            row.DeserializeBaseProperties(context, json);
            row.HorizontalCellContentAlignment = ParseUtil.GetOptionalEnumValue(json, "horizontalCellContentAlignment", EnumStrings.HAlignment);
            row.VerticalCellContentAlignment = ParseUtil.GetOptionalEnumValue(
                json, "verticalCellContentAlignment", EnumStrings.VerticalContentAlignment);
            row.Style = ParseUtil.GetEnumValue(json, "style", ContainerStyle.None, EnumStrings.ContainerStyle);
            row.Rtl = ParseUtil.GetOptionalBool(json, "rtl");

            foreach (var cell in ParseUtil.GetElementCollectionOfSingleType(context, json, "cells", AdaptiveTableCell.DeserializeTableCell, false))
            {
                row.Cells.Add(cell);
            }

            context.PopElement();
            return row;
        }
    }

    public sealed class AdaptiveTable : AdaptiveCardElementBase
    {
        public AdaptiveTable() : base(ElementType.Table, "Table")
        {
        }

        public bool ShowGridLines { get; set; } = true;

        public bool FirstRowAsHeaders { get; set; } = true;

        public HAlignment? HorizontalCellContentAlignment { get; set; }

        public VerticalContentAlignment? VerticalCellContentAlignment { get; set; }

        public ContainerStyle GridStyle { get; set; } = ContainerStyle.None;

        public IList<AdaptiveTableColumnDefinition> Columns { get; } = new List<AdaptiveTableColumnDefinition>();

        public IList<AdaptiveTableRow> Rows { get; } = new List<AdaptiveTableRow>();

        protected override void PopulateKnownProperties(ISet<string> knownProperties)
        {
            base.PopulateKnownProperties(knownProperties);
            knownProperties.Add("columns");
            knownProperties.Add("firstRowAsHeaders");
            knownProperties.Add("gridStyle");
            knownProperties.Add("horizontalCellContentAlignment");
            knownProperties.Add("rows");
            knownProperties.Add("showGridLines");
            knownProperties.Add("verticalCellContentAlignment");
        }

        public override JsonObject ToJson()
        {
            var root = base.ToJson();
            if (Columns.Count > 0)
            {
                var columns = new JsonArray();
                foreach (var column in Columns)
                {
                    columns.Add((JsonNode)column.ToJson());
                }

                root["columns"] = columns;
            }

            if (Rows.Count > 0)
            {
                var rows = new JsonArray();
                foreach (var row in Rows)
                {
                    rows.Add((JsonNode)row.ToJson());
                }

                root["rows"] = rows;
            }

            if (!ShowGridLines)
            {
                root["showGridLines"] = false;
            }

            if (!FirstRowAsHeaders)
            {
                root["firstRowAsHeaders"] = false;
            }

            if (GridStyle != ContainerStyle.None)
            {
                root["gridStyle"] = EnumStrings.ContainerStyle.ToString(GridStyle);
            }

            if (HorizontalCellContentAlignment.HasValue)
            {
                root["horizontalCellContentAlignment"] = EnumStrings.HAlignment.ToString(HorizontalCellContentAlignment.Value);
            }

            if (VerticalCellContentAlignment.HasValue)
            {
                root["verticalCellContentAlignment"] = EnumStrings.VerticalContentAlignment.ToString(VerticalCellContentAlignment.Value);
            }

            return root;
        }

        internal override void GetResourceInformation(List<AdaptiveRemoteResourceInformation> resourceInformation)
        {
            foreach (var row in Rows)
            {
                row.GetResourceInformation(resourceInformation);
            }
        }

        internal static AdaptiveTable Deserialize(ParseContext context, JsonObject json)
        {
            ParseUtil.ExpectTypeString(json, "Table");
            var table = new AdaptiveTable();
            table.DeserializeBaseProperties(context, json);

            // CollectionCoreElement::Deserialize
            bool canFallbackToAncestor = context.CanFallbackToAncestor;
            context.CanFallbackToAncestor = canFallbackToAncestor || table.FallbackType != FallbackType.None;
            table.CanFallbackToAncestor = canFallbackToAncestor;
            foreach (var row in ParseUtil.GetElementCollectionOfSingleType(context, json, "rows", AdaptiveTableRow.DeserializeTableRow, false))
            {
                table.Rows.Add(row);
            }

            context.CanFallbackToAncestor = canFallbackToAncestor;

            var columnsArray = ParseUtil.GetArray(json, "columns", false);
            if (columnsArray != null)
            {
                foreach (var columnJson in columnsArray)
                {
                    table.Columns.Add(AdaptiveTableColumnDefinition.Deserialize(context, columnJson));
                }
            }

            table.ShowGridLines = ParseUtil.GetBool(json, "showGridLines", true, false);
            table.GridStyle = ParseUtil.GetEnumValue(json, "gridStyle", ContainerStyle.None, EnumStrings.ContainerStyle);
            table.FirstRowAsHeaders = ParseUtil.GetBool(json, "firstRowAsHeaders", true, false);
            table.HorizontalCellContentAlignment = ParseUtil.GetOptionalEnumValue(json, "horizontalCellContentAlignment", EnumStrings.HAlignment);
            table.VerticalCellContentAlignment = ParseUtil.GetOptionalEnumValue(
                json, "verticalCellContentAlignment", EnumStrings.VerticalContentAlignment);
            return table;
        }
    }

    public sealed class AdaptiveTableParser : BuiltInElementParser
    {
        internal override IAdaptiveCardElement Deserialize(ParseContext context, JsonObject json) => AdaptiveTable.Deserialize(context, json);
    }
}
