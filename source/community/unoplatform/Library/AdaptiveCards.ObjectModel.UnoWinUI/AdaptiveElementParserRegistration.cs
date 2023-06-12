using System.Collections.Generic;
using Windows.Data.Json;

namespace AdaptiveCards.ObjectModel.WinUI3;

public class AdaptiveElementParserRegistration
{
    public AdaptiveElementParserRegistration()
    {
        RegisterDefaultElementParsers();

        // Register this (UWP) registration with a well known guid string in the shared model
        // registration so we can get it back again
        m_sharedParserRegistration->AddParser(::AdaptiveCards::ObjectModel::Xaml_OM::c_uwpElementParserRegistration,
                                              std::make_shared<::AdaptiveCards::ObjectModel::Xaml_OM::SharedModelElementParser>(*this));

        m_isInitializing = false;
    }

    //    void AdaptiveElementParserRegistration::Set(hstring const& type,
    //                                            winrt::AdaptiveCards::ObjectModel::Xaml_OM::IAdaptiveElementParser const& Parser)
    //{
    //    auto typeString = HStringToUTF8(type);

    //    // During initialization we will add the known parsers to m_registration. These are already present in the corresponding
    //    // shared model registration (m_sharedParserRegistration) which will throw if we attempt to modify them by adding them again.
    //    if (!m_isInitializing)
    //    {
    //        m_sharedParserRegistration->AddParser(
    //            typeString, std::make_shared<::AdaptiveCards::ObjectModel::Xaml_OM::SharedModelElementParser>(*this));
    //    }

    //(* m_registration)[typeString] = Parser;
    //}

    //winrt::AdaptiveCards::ObjectModel::Xaml_OM::IAdaptiveElementParser AdaptiveElementParserRegistration::Get(hstring const& type)
    //{
    //    auto found = m_registration->find(HStringToUTF8(type));
    //    if (found != m_registration->end())
    //    {
    //        return found->second;
    //    }
    //    return nullptr;
    //}

    //void AdaptiveElementParserRegistration::Remove(hstring const& type)
    //{
    //    std::string typeString = HStringToUTF8(type);

    //    m_sharedParserRegistration->RemoveParser(typeString);
    //    m_registration->erase(typeString);
    //}

    //std::shared_ptr<::AdaptiveCards::ElementParserRegistration> AdaptiveElementParserRegistration::GetSharedParserRegistration()
    //{
    //    return m_sharedParserRegistration;
    //}

    //private void RegisterDefaultElementParsers()
    //{
    //    Set("ActionSet", new AdaptiveActionSetParser());
    //    Set("Column", new AdaptiveColumnParser());
    //    Set("ColumnSet", new AdaptiveColumnSetParser());
    //    Set("Container", new AdaptiveContainerParser());
    //    Set("FactSet", new AdaptiveFactSetParser());
    //    Set("Image", new AdaptiveImageParser());
    //    Set("ImageSet", new AdaptiveImageSetParser());
    //    Set("Input.ChoiceSet", new AdaptiveChoiceSetInputParser());
    //    Set("Input.Date", new AdaptiveDateInputParser());
    //    Set("Input.Number", new AdaptiveNumberInputParser());
    //    Set("Input.Text", new AdaptiveTextInputParser());
    //    Set("Input.Time", new AdaptiveTimeInputParser());
    //    Set("Input.Toggle", new AdaptiveToggleInputParser());
    //    Set("Media", new AdaptiveMediaParser());
    //    Set("RichTextBlock", new AdaptiveRichTextBlockParser());
    //    Set("Table", new AdaptiveTableParser());
    //    Set("TextBlock", new AdaptiveTextBlockParser());
    //}

    internal static TAdaptiveCardElement FromJson<TAdaptiveCardElement, TSharedModelElement, TSharedModelParser>(
        JsonObject jsonObject,
        AdaptiveElementParserRegistration elementParserRegistration,
        AdaptiveActionParserRegistration actionParserRegistration,
        IList<AdaptiveWarning> adaptiveWarnings)
        TSharedModelParser : 
    {
        var elementParserRegistrationImpl = peek_innards<winrt::AdaptiveCards::ObjectModel::Xaml_OM::implementation::AdaptiveElementParserRegistration>(
                elementParserRegistration);
        var actionParserRegistrationImpl =
            peek_innards<winrt::AdaptiveCards::ObjectModel::Xaml_OM::implementation::AdaptiveActionParserRegistration>(actionParserRegistration);

        ParseContext context(elementParserRegistrationImpl->GetSharedParserRegistration(),
                             actionParserRegistrationImpl->GetSharedParserRegistration());

        TSharedModelParser parser;
        var baseCardElement = parser.DeserializeFromString(context, JsonObjectToString(jsonObject));
        SharedWarningsToAdaptiveWarnings(context.warnings, adaptiveWarnings);
        return winrt::make<TAdaptiveCardElement>(std::AdaptivePointerCast<TSharedModelElement>(baseCardElement));
    }
}
