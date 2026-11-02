// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
// Port of AdaptiveCardsSharedModelUnitTest/EverythingBagel.cpp
using System.Linq;
using AdaptiveCards.ObjectModel.Uno;
using Xunit;
using static AdaptiveCards.ObjectModel.Uno.Tests.TestUtil;

namespace AdaptiveCards.ObjectModel.Uno.Tests
{
    public class EverythingBagelTests
    {
        // NOTE: the C++ expectation serialized "role":"Link" on Action.OpenUrl. The C# port omits the role when it
        // equals the per-type default (Link for Action.OpenUrl), so those occurrences are removed in the test.
        private const string EverythingJson = "{\"actions\":[{\"data\":{\"submitValue\":true},\"id\":\"Action.Submit_id\",\"title\":\"Action.Submit\",\"tooltip\":\"tooltip\",\"type\":\"Action.Submit\"},{\"associatedInputs\":\"None\",\"data\":{\"Action.Execute_data_keyA\":\"Action.Execute_data_valueA\"},\"id\":\"Action.Execute_id\",\"isEnabled\":false,\"title\":\"Action.Execute_title\",\"type\":\"Action.Execute\",\"verb\":\"Action.Execute_verb\"},{\"card\":{\"actions\":[],\"backgroundImage\":{\"fillMode\":\"repeat\",\"horizontalAlignment\":\"right\",\"url\":\"https://adaptivecards.io/content/cats/1.png\",\"verticalAlignment\":\"center\"},\"body\":[{\"isSubtle\":true,\"text\":\"Action.ShowCard text\",\"type\":\"TextBlock\"}],\"lang\":\"en\",\"type\":\"AdaptiveCard\",\"version\":\"1.0\"},\"id\":\"Action.ShowCard_id\",\"title\":\"Action.ShowCard\",\"tooltip\":\"tooltip\",\"type\":\"Action.ShowCard\"}],\"authentication\":{\"buttons\":[{\"image\":\"authentication_buttons_0_image\",\"title\":\"authentication_buttons_0_title\",\"type\":\"authentication_buttons_0_type\",\"value\":\"authentication_buttons_0_value\"}],\"connectionName\":\"authentication_connectionName\",\"text\":\"authentication_text\",\"tokenExchangeResource\":{\"id\":\"authentication_tokenExchangeResource_id\",\"providerId\":\"authentication_tokenExchangeResource_providerId\",\"uri\":\"authentication_tokenExchangeResource_uri\"}},\"backgroundImage\":\"https://adaptivecards.io/content/cats/1.png\",\"body\":[{\"color\":\"Default\",\"horizontalAlignment\":\"left\",\"id\":\"TextBlock_id\",\"isSubtle\":false,\"italic\":true,\"maxLines\":1,\"size\":\"Default\",\"strikethrough\":true,\"style\":\"heading\",\"text\":\"TextBlock_text\",\"type\":\"TextBlock\",\"weight\":\"Default\"},{\"color\":\"Default\",\"fontType\":\"Monospace\",\"horizontalAlignment\":\"left\",\"id\":\"TextBlock_id_mono\",\"isSubtle\":false,\"italic\":true,\"maxLines\":1,\"size\":\"Default\",\"strikethrough\":true,\"text\":\"TextBlock_text\",\"type\":\"TextBlock\",\"weight\":\"Default\"},{\"color\":\"Default\",\"fontType\":\"Default\",\"horizontalAlignment\":\"left\",\"id\":\"TextBlock_id_def\",\"isSubtle\":false,\"italic\":true,\"maxLines\":1,\"size\":\"Default\",\"strikethrough\":true,\"text\":\"TextBlock_text\",\"type\":\"TextBlock\",\"weight\":\"Default\"},{\"altText\":\"Image_altText\",\"horizontalAlignment\":\"center\",\"id\":\"Image_id\",\"isVisible\":false,\"selectAction\":{\"role\":\"Link\",\"title\":\"Image_Action.OpenUrl\",\"type\":\"Action.OpenUrl\",\"url\":\"https://adaptivecards.io/\"},\"separator\":true,\"size\":\"Auto\",\"spacing\":\"none\",\"style\":\"person\",\"type\":\"Image\",\"url\":\"https://adaptivecards.io/content/cats/1.png\"},{\"id\":\"Container_id\",\"items\":[{\"columns\":[{\"id\":\"Column_id1\",\"items\":[{\"type\":\"Image\",\"url\":\"https://adaptivecards.io/content/cats/1.png\"}],\"rtl\":false,\"style\":\"Default\",\"type\":\"Column\",\"width\":\"auto\"},{\"id\":\"Column_id2\",\"items\":[{\"type\":\"Image\",\"url\":\"https://adaptivecards.io/content/cats/2.png\"}],\"style\":\"Emphasis\",\"type\":\"Column\",\"width\":\"20px\"},{\"id\":\"Column_id3\",\"items\":[{\"type\":\"Image\",\"url\":\"https://adaptivecards.io/content/cats/3.png\"},{\"id\":\"Column3_TextBlock_id\",\"text\":\"Column3_TextBlock_text\",\"type\":\"TextBlock\"}],\"style\":\"Default\",\"type\":\"Column\",\"width\":\"stretch\"}],\"id\":\"ColumnSet_id\",\"separator\":true,\"spacing\":\"large\",\"type\":\"ColumnSet\"}],\"rtl\":true,\"selectAction\":{\"data\":\"Container_data\",\"title\":\"Container_Action.Submit\",\"type\":\"Action.Submit\"},\"spacing\":\"medium\",\"style\":\"Default\",\"type\":\"Container\"},{\"facts\":[{\"title\":\"Topping\",\"value\":\"poppyseeds\"},{\"title\":\"Topping\",\"value\":\"onion flakes\"}],\"id\":\"FactSet_id\",\"type\":\"FactSet\"},{\"id\":\"ImageSet_id\",\"imageSize\":\"Auto\",\"images\":[{\"type\":\"Image\",\"url\":\"https://adaptivecards.io/content/cats/1.png\"},{\"type\":\"Image\",\"url\":\"https://adaptivecards.io/content/cats/2.png\"},{\"type\":\"Image\",\"url\":\"https://adaptivecards.io/content/cats/3.png\"}],\"separator\":true,\"type\":\"ImageSet\"},{\"id\":\"Container_id_inputs\",\"items\":[{\"id\":\"Input.Text_id\",\"inlineAction\":{\"iconUrl\":\"https://adaptivecards.io/content/cats/1.png\",\"title\":\"Input.Text_Action.Submit\",\"type\":\"Action.Submit\"},\"label\":\"Input.Text_label\",\"maxLength\":10,\"placeholder\":\"Input.Text_placeholder\",\"regex\":\"([A-Z])\\\\w+\",\"spacing\":\"small\",\"style\":\"text\",\"type\":\"Input.Text\",\"value\":\"Input.Text_value\"},{\"id\":\"Input.Number_id\",\"isRequired\":true,\"label\":\"Input.Number_label\",\"max\":9.5,\"min\":3.5,\"placeholder\":\"Input.Number_placeholder\",\"type\":\"Input.Number\",\"value\":4.5},{\"id\":\"Input.Date_id\",\"label\":\"Input.Date_label\",\"max\":\"1/1/2020\",\"min\":\"8/1/2018\",\"placeholder\":\"Input.Date_placeholder\",\"type\":\"Input.Date\",\"value\":\"8/9/2018\"},{\"errorMessage\":\"Input.Time.ErrorMessage\",\"id\":\"Input.Time_id\",\"isRequired\":true,\"label\":\"Input.Time_label\",\"max\":\"17:00\",\"min\":\"10:00\",\"placeholder\":\"Input.Time_placeholder\",\"type\":\"Input.Time\",\"value\":\"13:00\"},{\"id\":\"Input.Toggle_id\",\"label\":\"Input.Toggle_label\",\"title\":\"Input.Toggle_title\",\"type\":\"Input.Toggle\",\"value\":\"Input.Toggle_on\",\"valueOff\":\"Input.Toggle_off\",\"valueOn\":\"Input.Toggle_on\"},{\"size\":\"Large\",\"text\":\"Everybody's got choices\",\"type\":\"TextBlock\",\"weight\":\"Bolder\"},{\"choices\":[{\"title\":\"Input.Choice1_title\",\"value\":\"Input.Choice1\"},{\"title\":\"Input.Choice2_title\",\"value\":\"Input.Choice2\"},{\"title\":\"Input.Choice3_title\",\"value\":\"Input.Choice3\"},{\"title\":\"Input.Choice4_title\",\"value\":\"Input.Choice4\"}],\"id\":\"Input.ChoiceSet_id\",\"isMultiSelect\":true,\"label\":\"Input.ChoiceSet_label\",\"style\":\"Compact\",\"type\":\"Input.ChoiceSet\",\"value\":\"Input.Choice2,Input.Choice4\"}],\"type\":\"Container\"},{\"actions\":[{\"associatedInputs\":\"None\",\"id\":\"ActionSet.Action.Submit_id\",\"isEnabled\":false,\"title\":\"ActionSet.Action.Submit\",\"tooltip\":\"tooltip\",\"type\":\"Action.Submit\"},{\"id\":\"ActionSet.Action.OpenUrl_id\",\"role\":\"Link\",\"title\":\"ActionSet.Action.OpenUrl\",\"tooltip\":\"tooltip\",\"type\":\"Action.OpenUrl\",\"url\":\"https://adaptivecards.io/\"}],\"type\":\"ActionSet\"},{\"horizontalAlignment\":\"right\",\"id\":\"RichTextBlock_id\",\"inlines\":[{\"color\":\"Dark\",\"fontType\":\"Monospace\",\"highlight\":true,\"isSubtle\":true,\"italic\":true,\"size\":\"Large\",\"strikethrough\":true,\"text\":\"This is a text run\",\"type\":\"TextRun\",\"underline\":true,\"weight\":\"Bolder\"},{\"selectAction\":{\"type\":\"Action.Submit\"},\"text\":\"This is another text run\",\"type\":\"TextRun\"},{\"text\":\"This is a text run specified as a string\",\"type\":\"TextRun\"}],\"type\":\"RichTextBlock\"}],\"fallbackText\":\"fallbackText\",\"lang\":\"en\",\"refresh\":{\"action\":{\"id\":\"refresh_action_id\",\"type\":\"Action.Execute\",\"verb\":\"refresh_action_verb\"},\"userIds\":[\"refresh_userIds_0\"]},\"rtl\":false,\"speak\":\"speak\",\"type\":\"AdaptiveCard\",\"version\":\"1.0\"}\n";

        private static void ValidateBackgroundImage(AdaptiveBackgroundImage backImage, BackgroundImageFillMode mode, HAlignment hAlignment, VAlignment vAlignment)
        {
            Assert.Equal("https://adaptivecards.io/content/cats/1.png", backImage.Url);
            Assert.Equal(mode, backImage.FillMode);
            Assert.Equal(hAlignment, backImage.HorizontalAlignment);
            Assert.Equal(vAlignment, backImage.VerticalAlignment);
        }

        private static void ValidateRefresh(AdaptiveRefresh refresh)
        {
            Assert.NotNull(refresh.Action);
            Assert.Equal(ActionType.Execute, refresh.Action.ActionType);
            Assert.Equal("refresh_action_id", refresh.Action.Id);
            Assert.Single(refresh.UserIds);
            Assert.Equal("refresh_userIds_0", refresh.UserIds[0]);
        }

        private static void ValidateAuthentication(AdaptiveAuthentication authentication)
        {
            Assert.Equal("authentication_text", authentication.Text);
            Assert.Equal("authentication_connectionName", authentication.ConnectionName);
            Assert.NotNull(authentication.TokenExchangeResource);
            Assert.Equal("authentication_tokenExchangeResource_id", authentication.TokenExchangeResource.Id);
            Assert.Equal("authentication_tokenExchangeResource_uri", authentication.TokenExchangeResource.Uri);
            Assert.Equal("authentication_tokenExchangeResource_providerId", authentication.TokenExchangeResource.ProviderId);
            Assert.Single(authentication.Buttons);
            Assert.Equal("authentication_buttons_0_type", authentication.Buttons[0].Type);
            Assert.Equal("authentication_buttons_0_title", authentication.Buttons[0].Title);
            Assert.Equal("authentication_buttons_0_image", authentication.Buttons[0].Image);
            Assert.Equal("authentication_buttons_0_value", authentication.Buttons[0].Value);
        }

        private static void ValidateTopLevelProperties(AdaptiveCard card)
        {
            ValidateBackgroundImage(card.BackgroundImage, BackgroundImageFillMode.Cover, HAlignment.Left, VAlignment.Top);
            ValidateRefresh(card.Refresh);
            ValidateAuthentication(card.Authentication);
            Assert.Equal(ElementType.AdaptiveCard, card.ElementType);
            Assert.Equal("fallbackText", card.FallbackText);
            Assert.Equal(HeightType.Auto, card.Height);
            Assert.Equal("en", card.Language);
            Assert.Null(card.SelectAction);
            Assert.Equal("speak", card.Speak);
            Assert.Equal(ContainerStyle.None, card.Style);
            Assert.Equal("1.0", card.Version);
            Assert.False(card.Rtl.Value);
            Assert.Equal(VerticalContentAlignment.Top, card.VerticalContentAlignment);
        }

        private static void ValidateTextBlock(AdaptiveTextBlock textBlock, FontType? fontType, TextStyle? style, string id)
        {
            Assert.Equal(ElementType.TextBlock, textBlock.ElementType);
            Assert.Equal("TextBlock", textBlock.ElementTypeString);
            Assert.Equal(id, textBlock.Id);
            Assert.Equal("TextBlock_text", textBlock.Text);
            Assert.Equal(style, textBlock.Style);
            Assert.Equal(ForegroundColor.Default, textBlock.Color);
            Assert.Equal(HAlignment.Left, textBlock.HorizontalAlignment);
            Assert.Equal(Spacing.Default, textBlock.Spacing);
            Assert.Equal(1U, textBlock.MaxLines);
            Assert.Equal("en", textBlock.Language);
            Assert.Equal(TextSize.Default, textBlock.Size);
            Assert.Equal(TextWeight.Default, textBlock.Weight);
            Assert.Equal(fontType, textBlock.FontType);
            Assert.False(textBlock.IsSubtle.Value);
            Assert.False(textBlock.Separator);
            Assert.False(textBlock.Wrap);
        }

        private static void ValidateImage(AdaptiveImage image)
        {
            Assert.Equal(ElementType.Image, image.ElementType);
            Assert.Equal("Image", image.ElementTypeString);
            Assert.Equal("Image_id", image.Id);
            Assert.Equal("Image_altText", image.AltText);
            Assert.Equal("https://adaptivecards.io/content/cats/1.png", image.Url);
            Assert.Equal("", image.BackgroundColor);
            Assert.Equal(ImageStyle.Person, image.Style);
            Assert.Equal(Spacing.None, image.Spacing);
            Assert.Equal(HeightType.Auto, image.Height);
            Assert.Equal(HAlignment.Center, image.HorizontalAlignment);
            Assert.Equal(ImageSize.Auto, image.Size);
            Assert.True(image.Separator);
            Assert.False(image.IsVisible);
            var imageAction = Assert.IsType<AdaptiveOpenUrlAction>(image.SelectAction);
            Assert.Equal("Image_Action.OpenUrl", imageAction.Title);
            Assert.Equal("https://adaptivecards.io/", imageAction.Url.ToString());
            Assert.Equal(ActionType.OpenUrl, imageAction.ActionType);
            Assert.Equal("Action.OpenUrl", imageAction.ActionTypeString);
            Assert.True(imageAction.IsEnabled);
        }

        private static void ValidateColumnSet(AdaptiveColumnSet columnSet)
        {
            Assert.Equal(ElementType.ColumnSet, columnSet.ElementType);
            Assert.Equal("ColumnSet", columnSet.ElementTypeString);
            Assert.Equal("ColumnSet_id", columnSet.Id);
            Assert.Equal(Spacing.Large, columnSet.Spacing);
            Assert.True(columnSet.Separator);
            var columns = columnSet.Columns;
            Assert.Equal(3, columns.Count);

            var first = columns[0];
            Assert.Equal(ElementType.Column, first.ElementType);
            Assert.Equal("Column", first.ElementTypeString);
            Assert.Equal("Column_id1", first.Id);
            Assert.Equal("auto", first.Width);
            Assert.Equal(0U, first.PixelWidth);
            Assert.Equal(ContainerStyle.Default, first.Style);
            Assert.False(first.Rtl.Value);
            Assert.Single(first.Items);
            Assert.Equal("https://adaptivecards.io/content/cats/1.png", ((AdaptiveImage)first.Items[0]).Url);

            var second = columns[1];
            Assert.Equal("Column_id2", second.Id);
            Assert.Equal("20px", second.Width);
            Assert.Equal(20U, second.PixelWidth);
            Assert.Equal(ContainerStyle.Emphasis, second.Style);
            Assert.False(second.Rtl.HasValue);
            Assert.Single(second.Items);
            Assert.Equal("https://adaptivecards.io/content/cats/2.png", ((AdaptiveImage)second.Items[0]).Url);

            var third = columns[2];
            Assert.Equal("Column_id3", third.Id);
            Assert.Equal("stretch", third.Width);
            Assert.Equal(0U, third.PixelWidth);
            Assert.Equal(ContainerStyle.Default, third.Style);
            Assert.Equal(2, third.Items.Count);
            Assert.Equal("https://adaptivecards.io/content/cats/3.png", ((AdaptiveImage)third.Items[0]).Url);
            var textBlockItem = (AdaptiveTextBlock)third.Items[1];
            Assert.Equal("Column3_TextBlock_text", textBlockItem.Text);
            Assert.Equal("Column3_TextBlock_id", textBlockItem.Id);
        }

        private static void ValidateColumnSetContainer(AdaptiveContainer container)
        {
            Assert.Equal(ElementType.Container, container.ElementType);
            Assert.Equal("Container", container.ElementTypeString);
            Assert.Equal("Container_id", container.Id);
            Assert.Equal(Spacing.Medium, container.Spacing);
            Assert.Equal(ContainerStyle.Default, container.Style);
            Assert.True(container.Rtl.Value);

            var action = Assert.IsType<AdaptiveSubmitAction>(container.SelectAction);
            Assert.Equal("Container_Action.Submit", action.Title);
            Assert.Equal("\"Container_data\"", action.DataJson.ToJsonString());
            Assert.Equal(AssociatedInputs.Auto, action.AssociatedInputs);

            Assert.Single(container.Items);
            ValidateColumnSet((AdaptiveColumnSet)container.Items[0]);
        }

        private static void ValidateFactSet(AdaptiveFactSet factSet)
        {
            Assert.Equal(ElementType.FactSet, factSet.ElementType);
            Assert.Equal("FactSet", factSet.ElementTypeString);
            Assert.Equal("FactSet_id", factSet.Id);
            Assert.Equal(2, factSet.Facts.Count);
            Assert.Equal("Topping", factSet.Facts[0].Title);
            Assert.Equal("poppyseeds", factSet.Facts[0].Value);
            Assert.Equal("Topping", factSet.Facts[1].Title);
            Assert.Equal("onion flakes", factSet.Facts[1].Value);
        }

        private static void ValidateImageSet(AdaptiveImageSet imageSet)
        {
            Assert.Equal(ElementType.ImageSet, imageSet.ElementType);
            Assert.Equal("ImageSet", imageSet.ElementTypeString);
            Assert.Equal("ImageSet_id", imageSet.Id);
            Assert.Equal(ImageSize.Auto, imageSet.ImageSize);
            Assert.Equal(3, imageSet.Images.Count);
            for (int i = 0; i < imageSet.Images.Count; i++)
            {
                Assert.Equal(ElementType.Image, imageSet.Images[i].ElementType);
                Assert.Equal($"https://adaptivecards.io/content/cats/{i + 1}.png", imageSet.Images[i].Url);
            }
        }

        private static void ValidateInputText(AdaptiveTextInput textInput)
        {
            Assert.Equal(ElementType.TextInput, textInput.ElementType);
            Assert.Equal("Input.Text", textInput.ElementTypeString);
            Assert.Equal("Input.Text_id", textInput.Id);
            Assert.False(textInput.IsMultiline);
            Assert.False(textInput.IsRequired);
            Assert.Equal(10U, textInput.MaxLength);
            Assert.Equal("Input.Text_placeholder", textInput.Placeholder);
            Assert.Equal(Spacing.Small, textInput.Spacing);
            Assert.Equal(TextInputStyle.Text, textInput.TextInputStyle);
            Assert.Equal("Input.Text_value", textInput.Value);
            Assert.Empty(textInput.ErrorMessage);
            Assert.Equal("([A-Z])\\w+", textInput.Regex);
            Assert.Equal("Input.Text_label", textInput.Label);
            var inlineAction = Assert.IsType<AdaptiveSubmitAction>(textInput.InlineAction);
            Assert.Equal("Input.Text_Action.Submit", inlineAction.Title);
            Assert.Equal("https://adaptivecards.io/content/cats/1.png", inlineAction.IconUrl);
            Assert.Equal(AssociatedInputs.Auto, inlineAction.AssociatedInputs);
            Assert.True(inlineAction.IsEnabled);
        }

        private static void ValidateInputNumber(AdaptiveNumberInput numberInput)
        {
            Assert.Equal(ElementType.NumberInput, numberInput.ElementType);
            Assert.Equal("Input.Number", numberInput.ElementTypeString);
            Assert.Equal("Input.Number_id", numberInput.Id);
            Assert.True(numberInput.IsRequired);
            Assert.Equal(9.5, numberInput.Max.Value);
            Assert.Equal(3.5, numberInput.Min.Value);
            Assert.Equal(4.5, numberInput.Value.Value);
            Assert.Equal("Input.Number_placeholder", numberInput.Placeholder);
            Assert.Empty(numberInput.ErrorMessage);
            Assert.Equal("Input.Number_label", numberInput.Label);
        }

        private static void ValidateInputDate(AdaptiveDateInput dateInput)
        {
            Assert.Equal(ElementType.DateInput, dateInput.ElementType);
            Assert.Equal("Input.Date", dateInput.ElementTypeString);
            Assert.Equal("Input.Date_id", dateInput.Id);
            Assert.Equal("1/1/2020", dateInput.Max);
            Assert.Equal("8/1/2018", dateInput.Min);
            Assert.Equal("8/9/2018", dateInput.Value);
            Assert.Equal("Input.Date_placeholder", dateInput.Placeholder);
            Assert.False(dateInput.IsRequired);
            Assert.Empty(dateInput.ErrorMessage);
            Assert.Equal("Input.Date_label", dateInput.Label);
        }

        private static void ValidateInputTime(AdaptiveTimeInput timeInput)
        {
            Assert.Equal(ElementType.TimeInput, timeInput.ElementType);
            Assert.Equal("Input.Time", timeInput.ElementTypeString);
            Assert.Equal("Input.Time_id", timeInput.Id);
            Assert.Equal("10:00", timeInput.Min);
            Assert.Equal("17:00", timeInput.Max);
            Assert.Equal("13:00", timeInput.Value);
            Assert.True(timeInput.IsRequired);
            Assert.Equal("Input.Time.ErrorMessage", timeInput.ErrorMessage);
            Assert.Equal("Input.Time_label", timeInput.Label);
        }

        private static void ValidateInputToggle(AdaptiveToggleInput toggleInput)
        {
            Assert.Equal(ElementType.ToggleInput, toggleInput.ElementType);
            Assert.Equal("Input.Toggle", toggleInput.ElementTypeString);
            Assert.Equal("Input.Toggle_id", toggleInput.Id);
            Assert.Equal("Input.Toggle_title", toggleInput.Title);
            Assert.Equal("Input.Toggle_on", toggleInput.Value);
            Assert.Equal("Input.Toggle_on", toggleInput.ValueOn);
            Assert.Equal("Input.Toggle_off", toggleInput.ValueOff);
            Assert.False(toggleInput.IsRequired);
            Assert.Empty(toggleInput.ErrorMessage);
            Assert.Equal("Input.Toggle_label", toggleInput.Label);
        }

        private static void ValidateTextBlockInInput(AdaptiveTextBlock textBlock)
        {
            Assert.Equal(ElementType.TextBlock, textBlock.ElementType);
            Assert.Equal("", textBlock.Id);
            Assert.Equal("Everybody's got choices", textBlock.Text);
            Assert.Equal(TextWeight.Bolder, textBlock.Weight);
            Assert.Equal(TextSize.Large, textBlock.Size);
        }

        private static void ValidateInputChoiceSet(AdaptiveChoiceSetInput choiceSet)
        {
            Assert.Equal(ElementType.ChoiceSetInput, choiceSet.ElementType);
            Assert.Equal("Input.ChoiceSet", choiceSet.ElementTypeString);
            Assert.Equal("Input.ChoiceSet_id", choiceSet.Id);
            Assert.Equal(ChoiceSetStyle.Compact, choiceSet.ChoiceSetStyle);
            Assert.Equal("Input.Choice2,Input.Choice4", choiceSet.Value);
            Assert.True(choiceSet.IsMultiSelect);
            Assert.False(choiceSet.IsRequired);
            Assert.Empty(choiceSet.ErrorMessage);
            Assert.Equal(4, choiceSet.Choices.Count);
            for (int i = 0; i < choiceSet.Choices.Count; i++)
            {
                Assert.Equal($"Input.Choice{i + 1}", choiceSet.Choices[i].Value);
                Assert.Equal($"Input.Choice{i + 1}_title", choiceSet.Choices[i].Title);
            }

            Assert.Equal("Input.ChoiceSet_label", choiceSet.Label);
        }

        private static void ValidateInputContainer(AdaptiveContainer container)
        {
            Assert.Equal("Container_id_inputs", container.Id);
            Assert.False(container.Rtl.HasValue);
            Assert.Equal(7, container.Items.Count);
            ValidateInputText((AdaptiveTextInput)container.Items[0]);
            ValidateInputNumber((AdaptiveNumberInput)container.Items[1]);
            ValidateInputDate((AdaptiveDateInput)container.Items[2]);
            ValidateInputTime((AdaptiveTimeInput)container.Items[3]);
            ValidateInputToggle((AdaptiveToggleInput)container.Items[4]);
            ValidateTextBlockInInput((AdaptiveTextBlock)container.Items[5]);
            ValidateInputChoiceSet((AdaptiveChoiceSetInput)container.Items[6]);
        }

        private static void ValidateActionSet(AdaptiveActionSet actionSet)
        {
            Assert.Equal(2, actionSet.Actions.Count);
            var submitAction = actionSet.Actions[0];
            Assert.Equal("ActionSet.Action.Submit_id", submitAction.Id);
            Assert.Equal(AssociatedInputs.None, ((AdaptiveSubmitAction)submitAction).AssociatedInputs);
            Assert.Equal("tooltip", submitAction.Tooltip);
            Assert.False(submitAction.IsEnabled);
            var openUrlAction = actionSet.Actions[1];
            Assert.Equal("ActionSet.Action.OpenUrl_id", openUrlAction.Id);
            Assert.Equal("tooltip", openUrlAction.Tooltip);
            Assert.True(openUrlAction.IsEnabled);
        }

        private static void ValidateRichTextBlock(AdaptiveRichTextBlock richTextBlock)
        {
            Assert.Equal(ElementType.RichTextBlock, richTextBlock.ElementType);
            Assert.Equal("RichTextBlock", richTextBlock.ElementTypeString);
            Assert.Equal("RichTextBlock_id", richTextBlock.Id);
            Assert.Equal(HAlignment.Right, richTextBlock.HorizontalAlignment);
            Assert.Equal(3, richTextBlock.Inlines.Count);

            var run = Assert.IsType<AdaptiveTextRun>(richTextBlock.Inlines[0]);
            Assert.Equal("This is a text run", run.Text);
            Assert.Equal(ForegroundColor.Dark, run.Color);
            Assert.Equal("en", run.Language);
            Assert.Equal(TextSize.Large, run.Size);
            Assert.Equal(TextWeight.Bolder, run.Weight);
            Assert.Equal(FontType.Monospace, run.FontType);
            Assert.True(run.IsSubtle.Value);
            Assert.True(run.Italic);
            Assert.True(run.Highlight);
            Assert.True(run.Strikethrough);
            Assert.True(run.Underline);

            run = Assert.IsType<AdaptiveTextRun>(richTextBlock.Inlines[1]);
            Assert.NotNull(run.SelectAction);
            Assert.Equal(ActionType.Submit, run.SelectAction.ActionType);
            Assert.Equal(AssociatedInputs.Auto, ((AdaptiveSubmitAction)run.SelectAction).AssociatedInputs);

            run = Assert.IsType<AdaptiveTextRun>(richTextBlock.Inlines[2]);
            Assert.Equal("This is a text run specified as a string", run.Text);
        }

        private static void ValidateBody(AdaptiveCard card)
        {
            var body = card.Body;
            Assert.Equal(10, body.Count);
            ValidateTextBlock((AdaptiveTextBlock)body[0], null, TextStyle.Heading, "TextBlock_id");
            ValidateTextBlock((AdaptiveTextBlock)body[1], FontType.Monospace, null, "TextBlock_id_mono");
            ValidateTextBlock((AdaptiveTextBlock)body[2], FontType.Default, null, "TextBlock_id_def");
            ValidateImage((AdaptiveImage)body[3]);
            ValidateColumnSetContainer((AdaptiveContainer)body[4]);
            ValidateFactSet((AdaptiveFactSet)body[5]);
            ValidateImageSet((AdaptiveImageSet)body[6]);
            ValidateInputContainer((AdaptiveContainer)body[7]);
            ValidateActionSet((AdaptiveActionSet)body[8]);
            ValidateRichTextBlock((AdaptiveRichTextBlock)body[9]);
        }

        private static void ValidateToplevelActions(AdaptiveCard card)
        {
            var actions = card.Actions;
            Assert.Equal(3, actions.Count);

            var submitAction = Assert.IsType<AdaptiveSubmitAction>(actions[0]);
            Assert.Equal(ActionType.Submit, submitAction.ActionType);
            Assert.Equal("Action.Submit", submitAction.ActionTypeString);
            Assert.Equal("", submitAction.IconUrl);
            Assert.Equal("Action.Submit_id", submitAction.Id);
            Assert.Equal("Action.Submit", submitAction.Title);
            Assert.Equal("{\"submitValue\":true}", Canonical(submitAction.DataJson));
            Assert.Equal(AssociatedInputs.Auto, submitAction.AssociatedInputs);
            Assert.Equal("tooltip", submitAction.Tooltip);
            Assert.True(submitAction.IsEnabled);
            Assert.Empty(submitAction.AdditionalProperties);
            Assert.Empty(submitAction.GetResourceInformation());

            var executeAction = Assert.IsType<AdaptiveExecuteAction>(actions[1]);
            Assert.Equal(ActionType.Execute, executeAction.ActionType);
            Assert.Equal("Action.Execute", executeAction.ActionTypeString);
            Assert.Equal("", executeAction.IconUrl);
            Assert.Equal("Action.Execute_id", executeAction.Id);
            Assert.Equal("Action.Execute_title", executeAction.Title);
            Assert.Equal("Action.Execute_verb", executeAction.Verb);
            Assert.Equal("{\"Action.Execute_data_keyA\":\"Action.Execute_data_valueA\"}", Canonical(executeAction.DataJson));
            Assert.Equal(AssociatedInputs.None, executeAction.AssociatedInputs);
            Assert.False(executeAction.IsEnabled);
            Assert.Empty(executeAction.AdditionalProperties);
            Assert.Empty(executeAction.GetResourceInformation());

            var showCardAction = Assert.IsType<AdaptiveShowCardAction>(actions[2]);
            Assert.Equal(ActionType.ShowCard, showCardAction.ActionType);
            Assert.Equal("Action.ShowCard", showCardAction.ActionTypeString);
            Assert.Equal("", showCardAction.IconUrl);
            Assert.Equal("Action.ShowCard_id", showCardAction.Id);
            Assert.Equal("Action.ShowCard", showCardAction.Title);
            Assert.Equal("tooltip", showCardAction.Tooltip);
            Assert.True(showCardAction.IsEnabled);
            Assert.Empty(showCardAction.AdditionalProperties);
            Assert.Single(showCardAction.GetResourceInformation());

            var subCard = showCardAction.Card;
            Assert.Empty(subCard.Actions);
            ValidateBackgroundImage(subCard.BackgroundImage, BackgroundImageFillMode.Repeat, HAlignment.Right, VAlignment.Center);
            Assert.Equal(ElementType.AdaptiveCard, subCard.ElementType);
            Assert.Equal("", subCard.FallbackText);
            Assert.Equal(HeightType.Auto, subCard.Height);
            Assert.Equal("en", subCard.Language);
            Assert.Null(subCard.SelectAction);
            Assert.Equal("", subCard.Speak);
            Assert.Equal(ContainerStyle.None, subCard.Style);
            Assert.Equal("1.0", subCard.Version);
            Assert.Equal(VerticalContentAlignment.Top, subCard.VerticalContentAlignment);
            Assert.Equal(
                "{\"actions\":[],\"backgroundImage\":{\"fillMode\":\"repeat\",\"horizontalAlignment\":\"right\",\"url\":\"https://adaptivecards.io/content/cats/1.png\",\"verticalAlignment\":\"center\"},\"body\":[{\"isSubtle\":true,\"text\":\"Action.ShowCard text\",\"type\":\"TextBlock\"}],\"lang\":\"en\",\"type\":\"AdaptiveCard\",\"version\":\"1.0\"}",
                Serialize(subCard));
        }

        private static void ValidateFallbackCard()
        {
            var fallbackCard = AdaptiveCard.MakeFallbackTextCard("fallback", "en", "speak");
            var fallbackTextBlock = (AdaptiveTextBlock)fallbackCard.Body[0];
            Assert.Equal("fallback", fallbackTextBlock.Text);
            Assert.Equal("en", fallbackTextBlock.Language);
            Assert.Equal("speak", fallbackCard.Speak);
        }

        [Fact]
        public void EverythingBagelTest()
        {
            var (card, context) = Deserialize(ReadTestData("EverythingBagel.json"), "1.0");
            Assert.Empty(context.Warnings);

            ValidateTopLevelProperties(card);
            ValidateBody(card);
            ValidateToplevelActions(card);
            ValidateFallbackCard();

            // Input.Text "style":"text" was round-tripped by the C++ model only through additional properties (style was
            // not a known property there). The C# port treats style as known and omits the default value.
            var expected = Cpp(EverythingJson)
                .Replace("\"role\":\"Link\",", string.Empty)
                .Replace("\"spacing\":\"small\",\"style\":\"text\",", "\"spacing\":\"small\",");
            Assert.Equal(expected, Serialize(card));
        }
    }
}
