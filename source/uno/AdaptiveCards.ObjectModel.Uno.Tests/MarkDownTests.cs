// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.
// Ported from source/shared/cpp/AdaptiveCardsSharedModel/AdaptiveCardsSharedModelUnitTest/MarkDownUnitTest.cpp
using AdaptiveCards.ObjectModel.Uno;
using Xunit;

namespace AdaptiveCards.ObjectModel.Uno.Tests
{
    public class MarkdownTests
    {
        [Fact]
        public void MarkDownBasicSanityTest_CanHandleEmptyStringTest()
        {
            var parser = new MarkDownParser("");
            Assert.Equal("<p></p>", parser.TransformToHtml());
            Assert.False(parser.HasHtmlTags());
        }

        [Fact]
        public void MarkDownBasicSanityTest_CanHandleEmphasisTest()
        {
            var parser = new MarkDownParser("*");
            Assert.Equal("<p>*</p>", parser.TransformToHtml());
            Assert.False(parser.HasHtmlTags());
        }

        [Fact]
        public void MarkDownBasicSanityTest_CanHandleStrongEmphasisTest()
        {
            var parser = new MarkDownParser("**");
            Assert.Equal("<p>**</p>", parser.TransformToHtml());
            Assert.False(parser.HasHtmlTags());
        }

        [Fact]
        public void EmphasisLeftDelimiterTest_LeftDelimiterTest()
        {
            var parser = new MarkDownParser("*foo bar*");
            Assert.Equal("<p><em>foo bar</em></p>", parser.TransformToHtml());
        }

        [Fact]
        public void EmphasisLeftDelimiterTest_UnderscoreLeftDelimiterTest()
        {
            var parser = new MarkDownParser("_foo bar_");
            Assert.Equal("<p><em>foo bar</em></p>", parser.TransformToHtml());
        }

        [Fact]
        public void EmphasisLeftDelimiterTest_UnderscoreLeftDelimiterFalseCaseWithSpaceTest()
        {
            var parser = new MarkDownParser("_ foo bar_");
            Assert.Equal("<p>_ foo bar_</p>", parser.TransformToHtml());
            Assert.False(parser.HasHtmlTags());
        }

        [Fact]
        public void EmphasisLeftDelimiterTest_LeftDelimiterFalseCaseWithAlphaNumericInfrontAndPuntuationBehind()
        {
            var parser = new MarkDownParser("a*\"foo\"*");
            Assert.Equal("<p>a*&quot;foo&quot;*</p>", parser.TransformToHtml());
        }

        [Fact]
        public void EmphasisLeftDelimiterTest_UnderscoreLeftDelimiterFalseCaseWithAlphaNumericInfrontAndPuntuationBehind()
        {
            var parser = new MarkDownParser("a_\"foo\"_");
            Assert.Equal("<p>a_&quot;foo&quot;_</p>", parser.TransformToHtml());
        }

        [Fact]
        public void EmphasisLeftDelimiterTest_LeftDelimiterIntraWordEmphasis()
        {
            var parser = new MarkDownParser("foo*bar*");
            Assert.Equal("<p>foo<em>bar</em></p>", parser.TransformToHtml());
        }

        [Fact]
        public void EmphasisLeftDelimiterTest_UnderscoreLeftDelimiterIntraWordEmphasis()
        {
            var parser = new MarkDownParser("foo_bar_");
            Assert.Equal("<p>foo_bar_</p>", parser.TransformToHtml());
        }

        [Fact]
        public void EmphasisLeftDelimiterTest_LeftDelimiterNumericIntraWordEmphasis()
        {
            var parser = new MarkDownParser("5*6*78");
            Assert.Equal("<p>5<em>6</em>78</p>", parser.TransformToHtml());
        }

        [Fact]
        public void EmphasisLeftDelimiterTest_UnderscoreLeftDelimiterNumericIntraWordEmphasis()
        {
            var parser = new MarkDownParser("5_6_78");
            Assert.Equal("<p>5_6_78</p>", parser.TransformToHtml());
        }

        [Fact]
        public void EmphasisLeftDelimiterTest_UnderscoreLeftDelimiterCanBeProceededAndFollowedByPunct()
        {
            var parser = new MarkDownParser("foo-_(bar)_");
            Assert.Equal("<p>foo-<em>(bar)</em></p>", parser.TransformToHtml());
        }

        [Fact]
        public void EmphasisDelimiterTest_MatchingRightDelimiterTest()
        {
            var parser = new MarkDownParser("_foo_");
            Assert.Equal("<p><em>foo</em></p>", parser.TransformToHtml());
        }

        [Fact]
        public void EmphasisDelimiterTest_NonMatchingDelimiterTest()
        {
            var parser = new MarkDownParser("_foo*");
            Assert.Equal("<p>_foo*</p>", parser.TransformToHtml());
        }

        [Fact]
        public void EmphasisDelimiterTest_MatchingRightDelimiterWithSpaceTest()
        {
            var parser = new MarkDownParser("*foo *");
            Assert.Equal("<p>*foo *</p>", parser.TransformToHtml());
        }

        [Fact]
        public void EmphasisDelimiterTest_ValidDelimitersSurroundedByPunctuationTest()
        {
            var parser = new MarkDownParser("*(*foo*)*");
            Assert.Equal("<p><em>(<em>foo</em>)</em></p>", parser.TransformToHtml());
        }

        [Fact]
        public void EmphasisDelimiterTest_PunctuationSurroundedByDelimiterValidTest()
        {
            var parser = new MarkDownParser("*(foo)*");
            Assert.Equal("<p><em>(foo)</em></p>", parser.TransformToHtml());
        }

        [Fact]
        public void EmphasisDelimiterTest_ValidIntraWordEmphasisTest()
        {
            var parser = new MarkDownParser("*foo*bar");
            Assert.Equal("<p><em>foo</em>bar</p>", parser.TransformToHtml());
        }

        [Fact]
        public void EmphasisDelimiterTest_WhiteSpaceClosingEmphasisInvalidTest()
        {
            var parser = new MarkDownParser("_foo bar _");
            Assert.Equal("<p>_foo bar _</p>", parser.TransformToHtml());
        }

        [Fact]
        public void EmphasisDelimiterTest_InvalidIntraWordEmphasisTest()
        {
            var parser = new MarkDownParser("_foo_bar");
            Assert.Equal("<p>_foo_bar</p>", parser.TransformToHtml());
        }

        [Fact]
        public void EmphasisDelimiterTest_RightDelimiterFollowedByPunctuationValidTest()
        {
            var parser = new MarkDownParser("_(bar)_.");
            Assert.Equal("<p><em>(bar)</em>.</p>", parser.TransformToHtml());
        }

        [Fact]
        public void StrongDelimiterTest_SimpleValidCaseTest()
        {
            var parser = new MarkDownParser("**foo bar**");
            Assert.Equal("<p><strong>foo bar</strong></p>", parser.TransformToHtml());
            var parser1 = new MarkDownParser("__foo bar__");
            Assert.Equal("<p><strong>foo bar</strong></p>", parser1.TransformToHtml());
        }

        [Fact]
        public void StrongDelimiterTest_DelimiterWithSpaceInvalidCaseTest()
        {
            var parser = new MarkDownParser("** foo bar**");
            Assert.Equal("<p>** foo bar**</p>", parser.TransformToHtml());
            var parser1 = new MarkDownParser("__ foo bar__");
            Assert.Equal("<p>__ foo bar__</p>", parser1.TransformToHtml());
            var parser2 = new MarkDownParser("**foo bar **");
            Assert.Equal("<p>**foo bar **</p>", parser2.TransformToHtml());
            var parser3 = new MarkDownParser("__foo bar __");
            Assert.Equal("<p>__foo bar __</p>", parser3.TransformToHtml());
        }

        [Fact]
        public void StrongDelimiterTest_DelimiterSurroundingPuntuationInvalidCaseTest()
        {
            var parser = new MarkDownParser("a**\"foo bar\"**");
            Assert.Equal("<p>a**&quot;foo bar&quot;**</p>", parser.TransformToHtml());
            var parser2 = new MarkDownParser("a__\"foo bar\"__");
            Assert.Equal("<p>a__&quot;foo bar&quot;__</p>", parser2.TransformToHtml());
        }

        [Fact]
        public void StrongDelimiterTest_IntraWordTest()
        {
            var parser = new MarkDownParser("**foo**bar");
            Assert.Equal("<p><strong>foo</strong>bar</p>", parser.TransformToHtml());
            var parser2 = new MarkDownParser("__foo__bar");
            Assert.Equal("<p>__foo__bar</p>", parser2.TransformToHtml());
        }

        [Fact]
        public void StrongDelimiterTest_PunctuationDelimitersTest()
        {
            var parser = new MarkDownParser("**(**foo)");
            Assert.Equal("<p>**(**foo)</p>", parser.TransformToHtml());

            var parser2 = new MarkDownParser("__(__foo");
            Assert.Equal("<p>__(__foo</p>", parser2.TransformToHtml());

            var parser3 = new MarkDownParser("foo-__(bar)__");
            Assert.Equal("<p>foo-<strong>(bar)</strong></p>", parser3.TransformToHtml());

            var parser4 = new MarkDownParser("(**foo**)");
            Assert.Equal("<p>(<strong>foo</strong>)</p>", parser4.TransformToHtml());

            var parser5 = new MarkDownParser("(__foo__)");
            Assert.Equal("<p>(<strong>foo</strong>)</p>", parser5.TransformToHtml());

            var parser6 = new MarkDownParser("__(foo)__.");
            Assert.Equal("<p><strong>(foo)</strong>.</p>", parser6.TransformToHtml());
        }

        [Fact]
        public void DelimiterNestingTest_PunctuationDelimitersTest()
        {
            var parser = new MarkDownParser("_foo __bar__ baz_");
            Assert.Equal("<p><em>foo <strong>bar</strong> baz</em></p>", parser.TransformToHtml());

            var parser2 = new MarkDownParser("*foo *bar**");
            Assert.Equal("<p><em>foo <em>bar</em></em></p>", parser2.TransformToHtml());

            var parser3 = new MarkDownParser("_foo _bar_ baz_");
            Assert.Equal("<p><em>foo <em>bar</em> baz</em></p>", parser3.TransformToHtml());

            var parser4 = new MarkDownParser("*foo **bar** baz*");
            Assert.Equal("<p><em>foo <strong>bar</strong> baz</em></p>", parser4.TransformToHtml());

            var parser5 = new MarkDownParser("*foo **bar *baz* bim** bop*");
            Assert.Equal("<p><em>foo <strong>bar <em>baz</em> bim</strong> bop</em></p>", parser5.TransformToHtml());

            var parser6 = new MarkDownParser("***foo** bar*");
            Assert.Equal("<p><em><strong>foo</strong> bar</em></p>", parser6.TransformToHtml());

            var parser7 = new MarkDownParser("*foo **bar***");
            Assert.Equal("<p><em>foo <strong>bar</strong></em></p>", parser7.TransformToHtml());

            var parser8 = new MarkDownParser("** is not an empty emphasis");
            Assert.Equal("<p>** is not an empty emphasis</p>", parser8.TransformToHtml());

            var parser9 = new MarkDownParser("**** is not an empty emphasis");
            Assert.Equal("<p>**** is not an empty emphasis</p>", parser9.TransformToHtml());

            var parser10 = new MarkDownParser("**foo\nbar**");
            Assert.Equal("<p><strong>foo\nbar</strong></p>", parser10.TransformToHtml());

            var parser11 = new MarkDownParser("__foo __bar__ baz__");
            Assert.Equal("<p><strong>foo <strong>bar</strong> baz</strong></p>", parser11.TransformToHtml());

            var parser12 = new MarkDownParser("**foo **bar****");
            Assert.Equal("<p><strong>foo <strong>bar</strong></strong></p>", parser12.TransformToHtml());

            var parser13 = new MarkDownParser("**foo *bar **baz**\n bim* bop**");
            Assert.Equal("<p><strong>foo <em>bar <strong>baz</strong>\n bim</em> bop</strong></p>", parser13.TransformToHtml());

        }

        [Fact]
        public void Rule11_12Test_EscapeTest()
        {
            var parser = new MarkDownParser("foo *\\**");
            Assert.Equal("<p>foo <em>*</em></p>", parser.TransformToHtml());

            var parser2 = new MarkDownParser("foo **\\***");
            Assert.Equal("<p>foo <strong>*</strong></p>", parser2.TransformToHtml());

            var parser3 = new MarkDownParser("foo __\\___");
            Assert.Equal("<p>foo <strong>_</strong></p>", parser3.TransformToHtml());
        }

        [Fact]
        public void Rule11_12Test_UnevenMatchingDelimiter()
        {
            var parser = new MarkDownParser("**foo*");
            Assert.Equal("<p>*<em>foo</em></p>", parser.TransformToHtml());

            var parser1 = new MarkDownParser("*foo**");
            Assert.Equal("<p><em>foo</em>*</p>", parser1.TransformToHtml());

            var parser2 = new MarkDownParser("***foo**");
            Assert.Equal("<p>*<strong>foo</strong></p>", parser2.TransformToHtml());

            var parser3 = new MarkDownParser("*foo****");
            Assert.Equal("<p><em>foo</em>***</p>", parser3.TransformToHtml());

            var parser4 = new MarkDownParser("**Gomphocarpus (*Gomphocarpus physocarpus*, syn.\n*Asclepias physocarpa*)**");
            Assert.Equal("<p><strong>Gomphocarpus (<em>Gomphocarpus physocarpus</em>, syn.\n<em>Asclepias physocarpa</em>)</strong></p>",
                parser4.TransformToHtml());

            var parser5 = new MarkDownParser("*Hello* abc ***Hello* def *world***");
            Assert.Equal("<p><em>Hello</em> abc <strong><em>Hello</em> def <em>world</em></strong></p>", parser5.TransformToHtml());

            var parser6 = new MarkDownParser("*foo**bar**baz*");
            Assert.Equal("<p><em>foo<strong>bar</strong>baz</em></p>", parser6.TransformToHtml());
        }

        [Fact]
        public void Rule13Test_strongEmphasisNesting()
        {
            var parser = new MarkDownParser("****foo****");
            Assert.Equal("<p><strong><strong>foo</strong></strong></p>", parser.TransformToHtml());
            var parser2 = new MarkDownParser("******foo******");
            Assert.Equal("<p><strong><strong><strong>foo</strong></strong></strong></p>", parser2.TransformToHtml());
        }

        [Fact]
        public void Rule14Test_strongAndEmphasisNesting()
        {
            var parser = new MarkDownParser("***foo***");
            Assert.Equal("<p><strong><em>foo</em></strong></p>", parser.TransformToHtml());

            var parser2 = new MarkDownParser("_____foo_____");
            Assert.Equal("<p><strong><strong><em>foo</em></strong></strong></p>", parser2.TransformToHtml());
        }

        [Fact]
        public void Rule15Test_OverlappingTest()
        {
            var parser = new MarkDownParser("*foo _bar* baz_");
            Assert.Equal("<p><em>foo _bar</em> baz_</p>", parser.TransformToHtml());

            var parser2 = new MarkDownParser("*foo __bar *baz bim__ bam*");
            Assert.Equal("<p><em>foo <strong>bar *baz bim</strong> bam</em></p>", parser2.TransformToHtml());
        }

        [Fact]
        public void Rule16Test_strongEmphasis()
        {
            var parser = new MarkDownParser("**foo **bar baz**");
            Assert.Equal("<p>**foo <strong>bar baz</strong></p>", parser.TransformToHtml());

            var parser2 = new MarkDownParser("*foo *bar baz*");
            Assert.Equal("<p>*foo <em>bar baz</em></p>", parser2.TransformToHtml());

            var parser3 = new MarkDownParser("**K *J *foo**bar* *cool*");
            Assert.Equal("<p><strong>K *J *foo</strong>bar* <em>cool</em></p>", parser3.TransformToHtml());

            var parser4 = new MarkDownParser("**m *J *foo**bar *cool**");
            Assert.Equal("<p><strong>m *J *foo</strong>bar <em>cool</em>*</p>", parser4.TransformToHtml());

            var parser5 = new MarkDownParser("**H *foo**bar***");
            Assert.Equal("<p><strong>H *foo</strong>bar***</p>", parser5.TransformToHtml());

            var parser6 = new MarkDownParser("*hello *hi **H foo** bar**");
            Assert.Equal("<p><em>hello <em>hi <strong>H foo</strong> bar</em></em></p>", parser6.TransformToHtml());

            var parser7 = new MarkDownParser("hello **how are** *you **i** am **great** *thank* **you***");
            Assert.Equal("<p>hello <strong>how are</strong> <em>you <strong>i</strong> am <strong>great</strong> <em>thank</em> <strong>you</strong></em></p>", parser7.TransformToHtml());

            var parser8 = new MarkDownParser("hello, **how are__ you**");
            Assert.Equal("<p>hello, <strong>how are__ you</strong></p>", parser8.TransformToHtml());

            var parser9 = new MarkDownParser("hello, __how **are__ you?**");
            Assert.Equal("<p>hello, <strong>how **are</strong> you?**</p>", parser9.TransformToHtml());

            var parser10 = new MarkDownParser("hello, **how are__ you**");
            Assert.Equal("<p>hello, <strong>how are__ you</strong></p>", parser10.TransformToHtml());
        }

        [Fact]
        public void Rule16Test_TempTest()
        {
            var parser11 = new MarkDownParser("*hello *hello**h*");
            Assert.Equal("<p>*hello <em>hello**h</em></p>", parser11.TransformToHtml());
        }

        [Fact]
        public void LinkBasicValidationTest_CanGenerateValidHtmlTagForLinkTest()
        {
            var parser = new MarkDownParser("[hello](www.naver.com)");
            Assert.Equal("<p><a href=\"www.naver.com\">hello</a></p>", parser.TransformToHtml());
        }

        [Fact]
        public void LinkBasicValidationTest_ValidLinkTestWithUnMatchingBrackets()
        {
            var parser = new MarkDownParser("[[[[hello](www.naver.com)");
            Assert.Equal("<p>[[[<a href=\"www.naver.com\">hello</a></p>", parser.TransformToHtml());
        }

        [Fact]
        public void LinkBasicValidationTest_ValidLinkTestWithMatchingInnerBrackets()
        {
            var parser = new MarkDownParser("[[hello]](www.naver.com)");
            Assert.Equal("<p><a href=\"www.naver.com\">[hello]</a></p>", parser.TransformToHtml());
        }

        [Fact]
        public void LinkBasicValidationTest_ValidLinkTestWithMatchingInnerBrackets2()
        {
            var parser = new MarkDownParser("[*[hello]*](www.naver.com)");
            Assert.Equal("<p><a href=\"www.naver.com\"><em>[hello]</em></a></p>", parser.TransformToHtml());
        }

        [Fact]
        public void LinkBasicValidationTest_ValidLinkTestWithMatchingInnerBrackets3()
        {
            var parser = new MarkDownParser("[*[hello[hello]hello]*](www.naver.com)");
            Assert.Equal("<p><a href=\"www.naver.com\"><em>[hello[hello]hello]</em></a></p>", parser.TransformToHtml());
        }

        [Fact]
        public void LinkBasicValidationTest_ValidLinkTestWithMatchingInnerBrackets4()
        {
            var parser = new MarkDownParser("[*hello[hello]hello](www.naver.com)");
            Assert.Equal("<p><a href=\"www.naver.com\">*hello[hello]hello</a></p>", parser.TransformToHtml());
        }

        [Fact]
        public void LinkBasicValidationTest_ValidLinkTestWithMatchingInnerBrackets5()
        {
            var parser = new MarkDownParser("[*hellohello]hello](www.naver.com)");
            Assert.Equal("<p>[*hellohello]hello](www.naver.com)</p>", parser.TransformToHtml());
        }

        [Fact]
        public void LinkBasicValidationTest_ValidLinkTestWithMatchingInnerBrackets6()
        {
            var parser = new MarkDownParser("[Bug [021356]](https://msn.com): Markdown link parsing");
            Assert.Equal("<p><a href=\"https://msn.com\">Bug [021356]</a>: Markdown link parsing</p>", parser.TransformToHtml());
        }

        [Fact]
        public void LinkBasicValidationTest_InvalidLinkTest()
        {
            var parser = new MarkDownParser("[hello(www.naver.com)");
            Assert.Equal("<p>[hello(www.naver.com)</p>", parser.TransformToHtml());
            Assert.False(parser.HasHtmlTags());
        }

        [Fact]
        public void LinkBasicValidationTest_InvalidLinkTestWithInvalidEmphasis()
        {
            var parser = new MarkDownParser("*[*hello(www.naver.com)");
            Assert.Equal("<p>*[*hello(www.naver.com)</p>", parser.TransformToHtml());
            Assert.False(parser.HasHtmlTags());
        }

        [Fact]
        public void LinkBasicValidationTest_InvalidLinkTestWithValidEmphasis()
        {
            var parser = new MarkDownParser("*[*hello(www.naver.com)*");
            Assert.Equal("<p>*[<em>hello(www.naver.com)</em></p>", parser.TransformToHtml());
            Assert.True(parser.HasHtmlTags());
        }

        [Fact]
        public void LinkBasicValidationTest_ValidLinkTestWithUnMatchingBracketsWithChars()
        {
            var parser = new MarkDownParser("[a[b[hello](www.naver.com)");
            Assert.Equal("<p>[a[b<a href=\"www.naver.com\">hello</a></p>", parser.TransformToHtml());
            Assert.True(parser.HasHtmlTags());
        }

        [Fact]
        public void LinkBasicValidationTest_ValidLinkTestWithUnMatchingBracketsWithCharsAndParenthesis()
        {
            var parser = new MarkDownParser("[[a[b[h(ello](www.naver.com)");
            Assert.Equal("<p>[[a[b<a href=\"www.naver.com\">h(ello</a></p>", parser.TransformToHtml());
            Assert.True(parser.HasHtmlTags());
        }

        [Fact]
        public void LinkBasicValidationTest_ValidLinkTestWithEscapedDelimiters()
        {
            var parser = new MarkDownParser("[[cool link!]](https://contoso.com/New%20Document%20(1\\).docx)");
            Assert.Equal("<p><a href=\"https://contoso.com/New%20Document%20(1).docx\">[cool link!]</a></p>", parser.TransformToHtml());
        }

        [Fact]
        public void LinkBasicValidationTest_LinkTextWithNumberAndPunchuations()
        {
            var parser = new MarkDownParser("[1234.5](www.naver.com)");
            Assert.Equal("<p><a href=\"www.naver.com\">1234.5</a></p>", parser.TransformToHtml());
            Assert.True(parser.HasHtmlTags());
        }

        [Fact]
        public void LinkBasicValidationTest_InvalidCharsBetweenLinkTextAndDestination()
        {
            var parser = new MarkDownParser("[hello]a(www.naver.com)");
            Assert.Equal("<p>[hello]a(www.naver.com)</p>", parser.TransformToHtml());
            Assert.False(parser.HasHtmlTags());
        }

        [Fact]
        public void LinkBasicValidationTest_OutSideEmphasisAndLinkTest()
        {
            var parser = new MarkDownParser("*[hello](www.naver.com)*");
            Assert.Equal("<p><em><a href=\"www.naver.com\">hello</a></em></p>", parser.TransformToHtml());
            Assert.True(parser.HasHtmlTags());
        }

        [Fact]
        public void LinkBasicValidationTest_EmphasisAndLinkTextTest()
        {
            var parser = new MarkDownParser("[*hello*](www.naver.com)");
            Assert.Equal("<p><a href=\"www.naver.com\"><em>hello</em></a></p>", parser.TransformToHtml());
        }

        [Fact]
        public void LinkBasicValidationTest_UmatchingEmphasisAndLinkTextTest()
        {
            var parser = new MarkDownParser("*[*hello*](www.naver.com)");
            Assert.Equal("<p>*<a href=\"www.naver.com\"><em>hello</em></a></p>", parser.TransformToHtml());
        }

        [Fact]
        public void LinkBasicValidationTest_EmphasisAndLinkDestinationTest()
        {
            var parser = new MarkDownParser("*[*hello*](*www.naver.com*)");
            Assert.Equal("<p>*<a href=\"*www.naver.com*\"><em>hello</em></a></p>", parser.TransformToHtml());
        }

        [Fact]
        public void LinkBasicValidationTest_LinkWithComplexEmphasisString()
        {
            var parser = new MarkDownParser("**Hello** *[*hello*](*www.naver.com*)*");
            Assert.Equal("<p><strong>Hello</strong> <em><a href=\"*www.naver.com*\"><em>hello</em></a></em></p>", parser.TransformToHtml());
        }

        [Fact]
        public void LinkBasicValidationTest_TwoLinksTest()
        {
            var parser = new MarkDownParser("*Hello* *[*hello*](*www.naver.com*)** Hello, [second](www.microsoft.com)");
            Assert.Equal("<p><em>Hello</em> <em><a href=\"*www.naver.com*\"><em>hello</em></a></em>* Hello, <a href=\"www.microsoft.com\">second</a></p>", parser.TransformToHtml());
        }

        [Fact]
        public void LinkNestedParenthesisTest_1()
        {
            var parser = new MarkDownParser("[empty destination]()");
            Assert.Equal("<p><a href=\"\">empty destination</a></p>", parser.TransformToHtml());
        }

        [Fact]
        public void LinkNestedParenthesisTest_2()
        {
            var parser = new MarkDownParser("[Stay tuned to Know [aaa] m (aa)ore](https://aaa.bbb.com/(a)sites(Test).doc?somegar)bageValue*afterstar()");
            Assert.Equal("<p><a href=\"https://aaa.bbb.com/(a)sites(Test).doc?somegar\">Stay tuned to Know [aaa] m (aa)ore</a>bageValue*afterstar()</p>", parser.TransformToHtml());
        }

        [Fact]
        public void LinkNestedParenthesisTest_3()
        {
            var parser = new MarkDownParser("[Stay tuned to Know more](https://aaa.bbb.com/(a)sites(Test).doc?somegarbageValue)*afterstar()");
            Assert.Equal("<p><a href=\"https://aaa.bbb.com/(a)sites(Test).doc?somegarbageValue\">Stay tuned to Know more</a>*afterstar()</p>", parser.TransformToHtml());
        }

        [Fact]
        public void LinkNestedParenthesisTest_4()
        {
            var parser = new MarkDownParser("[Stay tuned to Know more](https://aaa.bbb.com/(a)sit(es(Test).doc?somegarbageValue)*afafafafafafa)*");
            Assert.Equal("<p><a href=\"https://aaa.bbb.com/(a)sit(es(Test).doc?somegarbageValue)*afafafafafafa\">Stay tuned to Know more</a>*</p>", parser.TransformToHtml());
        }

        [Fact]
        public void LinkNestedParenthesisTest_5()
        {
            var parser = new MarkDownParser("[Stay tuned to Know more](https://aaa.bbb.com/(a)sit(es(Test).doc?somegarbageValue)*afafafafafafa)");
            Assert.Equal("<p><a href=\"https://aaa.bbb.com/(a)sit(es(Test).doc?somegarbageValue)*afafafafafafa\">Stay tuned to Know more</a></p>", parser.TransformToHtml());
        }

        [Fact]
        public void LinkNestedParenthesisTest_6()
        {
            var parser = new MarkDownParser("[Stay tuned to Know)]()afafafa()");
            Assert.Equal("<p><a href=\"\">Stay tuned to Know)</a>afafafa()</p>", parser.TransformToHtml());
        }

        [Fact]
        public void LinkNestedParenthesisTest_7()
        {
            var parser = new MarkDownParser("[](()");
            Assert.Equal("<p>[](()</p>", parser.TransformToHtml());
        }

        [Fact]
        public void ListTest_SimpleValidListTest()
        {
            var parser = new MarkDownParser("- hello");
            Assert.Equal("<ul><li>hello</li></ul>", parser.TransformToHtml());
            var parser2 = new MarkDownParser("* hello");
            Assert.Equal("<ul><li>hello</li></ul>", parser2.TransformToHtml());
            var parser3 = new MarkDownParser("+ hello");
            Assert.Equal("<ul><li>hello</li></ul>", parser3.TransformToHtml());
        }

        [Fact]
        public void ListTest_MultipleSimpleValidListTest()
        {
            var parser = new MarkDownParser("- hello\n- world\n- hi");
            Assert.Equal("<ul><li>hello</li><li>world</li><li>hi</li></ul>", parser.TransformToHtml());
            var parser2 = new MarkDownParser("* hello\n* world\n* hi");
            Assert.Equal("<ul><li>hello</li><li>world</li><li>hi</li></ul>", parser2.TransformToHtml());
            var parser3 = new MarkDownParser("* hello\n- Hi");
            Assert.Equal("<ul><li>hello</li><li>Hi</li></ul>", parser3.TransformToHtml());
            var parser4 = new MarkDownParser("+ hello\n+ world\n+ hi");
            Assert.Equal("<ul><li>hello</li><li>world</li><li>hi</li></ul>", parser4.TransformToHtml());
        }

        [Fact]
        public void ListTest_ListTestsWithInterHyphen()
        {
            var parser = new MarkDownParser("- hello world - hello hello");
            Assert.Equal("<ul><li>hello world - hello hello</li></ul>", parser.TransformToHtml());
            var parser2 = new MarkDownParser("* hello world - hello hello");
            Assert.Equal("<ul><li>hello world - hello hello</li></ul>", parser2.TransformToHtml());
            var parser3 = new MarkDownParser("- hello world + hello hello");
            Assert.Equal("<ul><li>hello world + hello hello</li></ul>", parser3.TransformToHtml());
        }

        [Fact]
        public void ListTest_MultipleListWithHyphenTests()
        {
            var parser = new MarkDownParser("- hello world - hello hello\r- winner winner chicken dinner");
            Assert.Equal("<ul><li>hello world - hello hello</li><li>winner winner chicken dinner</li></ul>", parser.TransformToHtml());
            var parser2 = new MarkDownParser("* hello world * hello hello\r* winner winner chicken dinner");
            Assert.Equal("<ul><li>hello world * hello hello</li><li>winner winner chicken dinner</li></ul>", parser2.TransformToHtml());
            var parser3 = new MarkDownParser("+ hello world * hello hello\r+ winner winner chicken dinner");
            Assert.Equal("<ul><li>hello world * hello hello</li><li>winner winner chicken dinner</li></ul>", parser3.TransformToHtml());
        }

        [Fact]
        public void ListTest_MultipleListWithHyphenAndEmphasisTests()
        {
            var parser = new MarkDownParser("- hello world - hello hello\r- ***winner* winner** chicken dinner");
            Assert.Equal("<ul><li>hello world - hello hello</li><li><strong><em>winner</em> winner</strong> chicken dinner</li></ul>", parser.TransformToHtml());
            var parser2 = new MarkDownParser("* hello world * hello hello\r* ***winner* winner** chicken dinner");
            Assert.Equal("<ul><li>hello world * hello hello</li><li><strong><em>winner</em> winner</strong> chicken dinner</li></ul>", parser2.TransformToHtml());
            var parser3 = new MarkDownParser("+ hello world * hello hello\r+ ***winner* winner** chicken dinner");
            Assert.Equal("<ul><li>hello world * hello hello</li><li><strong><em>winner</em> winner</strong> chicken dinner</li></ul>", parser3.TransformToHtml());
        }

        [Fact]
        public void ListTest_MultipleListWithLinkTest()
        {
            var parser = new MarkDownParser("- hello world\r- hello hello\r- new site = [adaptive card](www.adaptivecards.io)");
            Assert.Equal("<ul><li>hello world</li><li>hello hello</li><li>new site = <a href=\"www.adaptivecards.io\">adaptive card</a></li></ul>", parser.TransformToHtml());
            var parser2 = new MarkDownParser("* hello world\r* hello hello\r* new site = [adaptive card](www.adaptivecards.io)");
            Assert.Equal("<ul><li>hello world</li><li>hello hello</li><li>new site = <a href=\"www.adaptivecards.io\">adaptive card</a></li></ul>", parser2.TransformToHtml());
            var parser3 = new MarkDownParser("+ hello world\r+ hello hello\r+ new site = [adaptive card](www.adaptivecards.io)");
            Assert.Equal("<ul><li>hello world</li><li>hello hello</li><li>new site = <a href=\"www.adaptivecards.io\">adaptive card</a></li></ul>", parser3.TransformToHtml());
        }

        [Fact]
        public void ListTest_PtagedBlockElementFollowedByListTest()
        {
            var parser = new MarkDownParser("Hello\r- my list");
            Assert.Equal("<p>Hello</p><ul><li>my list</li></ul>", parser.TransformToHtml());
            Assert.True(parser.HasHtmlTags());
            var parser2 = new MarkDownParser("Hello\r* my list");
            Assert.Equal("<p>Hello</p><ul><li>my list</li></ul>", parser2.TransformToHtml());
            Assert.True(parser2.HasHtmlTags());
            var parser3 = new MarkDownParser("Hello\r+ my list");
            Assert.Equal("<p>Hello</p><ul><li>my list</li></ul>", parser3.TransformToHtml());
            Assert.True(parser3.HasHtmlTags());
        }

        [Fact]
        public void ListTest_ListFollowedByPtagedBlockElementTest()
        {
            var parser = new MarkDownParser("- my list\r\rHello");
            Assert.Equal("<ul><li>my list</li></ul><p>Hello</p>", parser.TransformToHtml());
            var parser2 = new MarkDownParser("* my list\r\rHello");
            Assert.Equal("<ul><li>my list</li></ul><p>Hello</p>", parser2.TransformToHtml());
            var parser3 = new MarkDownParser("+ my list\r\rHello");
            Assert.Equal("<ul><li>my list</li></ul><p>Hello</p>", parser3.TransformToHtml());
        }

        [Fact]
        public void ListTest_ListFollowedWithNewLineCharTest()
        {
            var parser = new MarkDownParser("- my list\rHello");
            Assert.Equal("<ul><li>my list\rHello</li></ul>", parser.TransformToHtml());
            var parser2 = new MarkDownParser("* my list\rHello");
            Assert.Equal("<ul><li>my list\rHello</li></ul>", parser2.TransformToHtml());
            var parser3 = new MarkDownParser("+ my list\rHello");
            Assert.Equal("<ul><li>my list\rHello</li></ul>", parser3.TransformToHtml());
        }

        [Fact]
        public void ListTest_InvalidListStringReturnedUnchangedTest()
        {
            var parser = new MarkDownParser("023-34-567");
            Assert.Equal("<p>023-34-567</p>", parser.TransformToHtml());
            Assert.False(parser.HasHtmlTags());
        }

        [Fact]
        public void ListTest_LeftDelimiterFalseCaseWithSpaceTest()
        {
            var parser = new MarkDownParser("* foo bar*");
            Assert.Equal("<ul><li>foo bar*</li></ul>", parser.TransformToHtml());
            Assert.True(parser.HasHtmlTags());
        }

        [Fact]
        public void OrderedListTest_SimpleValidListTest()
        {
            var parser = new MarkDownParser("1. hello");
            Assert.Equal("<ol start=\"1\"><li>hello</li></ol>", parser.TransformToHtml());
        }

        [Fact]
        public void OrderedListTest_MultipleSimpleValidListTest()
        {
            var parser = new MarkDownParser("1. hello\n2. Hi");
            Assert.Equal("<ol start=\"1\"><li>hello</li><li>Hi</li></ol>", parser.TransformToHtml());
        }

        [Fact]
        public void OrderedListTest_ListTestsWithInterHyphen()
        {
            var parser = new MarkDownParser("1. hello world - hello hello");
            Assert.Equal("<ol start=\"1\"><li>hello world - hello hello</li></ol>", parser.TransformToHtml());
            Assert.True(parser.HasHtmlTags());
        }

        [Fact]
        public void OrderedListTest_MultipleListWithHyphenTests()
        {
            var parser = new MarkDownParser("1. hello world - hello hello\r2. winner winner chicken dinner");
            Assert.Equal("<ol start=\"1\"><li>hello world - hello hello</li><li>winner winner chicken dinner</li></ol>", parser.TransformToHtml());
        }

        [Fact]
        public void OrderedListTest_MultipleListWithHyphenAndEmphasisTests()
        {
            var parser = new MarkDownParser("1. hello world - hello hello\r- ***winner* winner** chicken dinner");
            Assert.Equal("<ol start=\"1\"><li>hello world - hello hello</li></ol><ul><li><strong><em>winner</em> winner</strong> chicken dinner</li></ul>", parser.TransformToHtml());
        }

        [Fact]
        public void OrderedListTest_MultipleListWithLinkTest()
        {
            var parser = new MarkDownParser("1. hello world\r2. hello hello\r3. new site = [adaptive card](www.adaptivecards.io)");
            Assert.Equal("<ol start=\"1\"><li>hello world</li><li>hello hello</li><li>new site = <a href=\"www.adaptivecards.io\">adaptive card</a></li></ol>", parser.TransformToHtml());
        }

        [Fact]
        public void OrderedListTest_PtagedBlockElementFollowedByListTest()
        {
            var parser = new MarkDownParser("Hello\r1. my list");
            Assert.Equal("<p>Hello</p><ol start=\"1\"><li>my list</li></ol>", parser.TransformToHtml());
            Assert.True(parser.HasHtmlTags());
        }

        [Fact]
        public void OrderedListTest_ListFollowedByPtagedBlockElementTest()
        {
            var parser = new MarkDownParser("1. my list\r\rHello");
            Assert.Equal("<ol start=\"1\"><li>my list</li></ol><p>Hello</p>", parser.TransformToHtml());
        }

        [Fact]
        public void OrderedListTest_ListFollowedWithNewLineCharTest()
        {
            var parser = new MarkDownParser("1. my list\rHello");
            Assert.Equal("<ol start=\"1\"><li>my list\rHello</li></ol>", parser.TransformToHtml());
        }

        [Fact]
        public void OrderedListTest_ListStartsWithRandomNumberTest()
        {
            var parser = new MarkDownParser("777. my list\rHello");
            Assert.Equal("<ol start=\"777\"><li>my list\rHello</li></ol>", parser.TransformToHtml());
        }

        [Fact]
        public void EscapeHtmlCharactersTest_GreaterThanTest()
        {
            var parser = new MarkDownParser("5>3");
            Assert.Equal("<p>5&gt;3</p>", parser.TransformToHtml());
        }

        [Fact]
        public void EscapeHtmlCharactersTest_LessThanTest()
        {
            var parser = new MarkDownParser("3<5");
            Assert.Equal("<p>3&lt;5</p>", parser.TransformToHtml());
        }

        [Fact]
        public void EscapeHtmlCharactersTest_QuotationTest()
        {
            var parser = new MarkDownParser("\"Hello World!\"");
            Assert.Equal("<p>&quot;Hello World!&quot;</p>", parser.TransformToHtml());
        }

        [Fact]
        public void EscapeHtmlCharactersTest_AmpersandTest()
        {
            var parser = new MarkDownParser("Green Eggs & Ham");
            Assert.Equal("<p>Green Eggs &amp; Ham</p>", parser.TransformToHtml());
        }

        [Fact]
        public void NonLatinCharacters_NoMarkdown()
        {
            var parser = new MarkDownParser("以前の製品のリンクで検索");
            Assert.Equal("<p>以前の製品のリンクで検索</p>", parser.TransformToHtml());
        }

        [Fact]
        public void NonLatinCharacters_Bold()
        {
            var parser = new MarkDownParser("**以前の製品のリンクで検索**");
            Assert.Equal("<p><strong>以前の製品のリンクで検索</strong></p>", parser.TransformToHtml());
        }

        [Fact]
        public void NonLatinCharacters_BoldSentence()
        {
            var parser = new MarkDownParser("How about **以前の製品のリンクで検索**");
            Assert.Equal("<p>How about <strong>以前の製品のリンクで検索</strong></p>", parser.TransformToHtml());
        }

        [Fact]
        public void NonLatinCharacters_BoldSentenceLeadingNonLatin()
        {
            var parser = new MarkDownParser("以前の製品のリンクで検索 **以前の製品のリンクで検索**");
            Assert.Equal("<p>以前の製品のリンクで検索 <strong>以前の製品のリンクで検索</strong></p>", parser.TransformToHtml());
        }

        [Fact]
        public void NonLatinCharacters_BoldSentenceLeadingNonLatinNoSpace()
        {
            var parser = new MarkDownParser("以前の製品のリンクで検索**以前の製品のリンクで検索**");
            Assert.Equal("<p>以前の製品のリンクで検索<strong>以前の製品のリンクで検索</strong></p>", parser.TransformToHtml());
        }

        [Fact]
        public void NonLatinCharacters_BoldSentenceNoWhitespace()
        {
            var parser = new MarkDownParser("How about**以前の製品のリンクで検索**");
            Assert.Equal("<p>How about<strong>以前の製品のリンクで検索</strong></p>", parser.TransformToHtml());
        }

        [Fact]
        public void NonLatinCharacters_NewlineWithLink()
        {
            var parser = new MarkDownParser("It's OK!\rClick [以前の製品のリンクで検索](https://www.microsoft.com)\rClick [以前の製品のリンクで検索](https://www.microsoft.com)");
            Assert.Equal("<p>It's OK!\rClick <a href=\"https://www.microsoft.com\">以前の製品のリンクで検索</a>\rClick <a href=\"https://www.microsoft.com\">以前の製品のリンクで検索</a></p>",
                parser.TransformToHtml());
        }

        [Fact]
        public void NonLatinCharacters_NumberedListWithLink()
        {
            var parser = new MarkDownParser("1. Click [以前の製品のリンクで検索](https://www.microsoft.com)\r2. Click [以前の製品のリンクで検索](https://www.microsoft.com)");
            Assert.Equal("<ol start=\"1\"><li>Click <a href=\"https://www.microsoft.com\">以前の製品のリンクで検索</a></li><li>Click <a href=\"https://www.microsoft.com\">以前の製品のリンクで検索</a></li></ol>",
                parser.TransformToHtml());
        }

        [Fact]
        public void NonLatinCharacters_NumberedListWithLinkLeadingText()
        {
            var parser = new MarkDownParser("It's not OK!\r1. Click [以前の製品のリンクで検索](https://www.microsoft.com)\r2. Click [以前の製品のリンクで検索](https://www.microsoft.com)");
            Assert.Equal("<p>It's not OK!</p><ol start=\"1\"><li>Click <a href=\"https://www.microsoft.com\">以前の製品のリンクで検索</a></li><li>Click <a href=\"https://www.microsoft.com\">以前の製品のリンクで検索</a></li></ol>",
                parser.TransformToHtml());
        }

        [Fact]
        public void EscapeHtmlCharactersTest_CanDetectEscapeTest()
        {
            var parser = new MarkDownParser("");
            Assert.False(parser.IsEscaped());

            _ = parser.TransformToHtml();
            Assert.False(parser.IsEscaped());

            var parser1 = new MarkDownParser("&");
            _ = parser1.TransformToHtml();
            Assert.True(parser1.IsEscaped());

            var parser2 = new MarkDownParser("Hello World&");
            _ = parser2.TransformToHtml();
            Assert.True(parser2.IsEscaped());

            var parser3 = new MarkDownParser(" & ");
            _ = parser3.TransformToHtml();
            Assert.True(parser3.IsEscaped());
        }

        [Fact]
        public void Rule9Test_MultipleOf3Test()
        {
            Assert.Equal("<p>Hello***World***</p>", new MarkDownParser("Hello***World***").TransformToHtml());
        }
    }
}
