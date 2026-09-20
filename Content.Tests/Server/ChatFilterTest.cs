using Content.Server._Crescent.Chat;
using Content.Shared._Crescent.Chat;
using NUnit.Framework;

namespace Content.Tests.Server;

[TestFixture]
public sealed class ChatFilterTest
{
    private static readonly ChatFilterMatcher Matcher = new(new ChatFilterPrototype
    {
        Anywhere = { "nigger", "retard" },
        InWord = { "fuck", "shit", "cunt", "molest", "incest" },
        WholeWord = { "spic", "dick", "dyke", "rape", "raping", "rapist", "pedo" },
        Allowed = { "retardant", "scunthorpe", "shiitake", "spice", "van dyke" },
    });

    private static string? Match(string text) => Matcher.Match(ChatFilterMatcher.Normalize(text));

    [TestCase("nigger")]
    [TestCase("NIGGER")]
    [TestCase("n1gg3r")]
    [TestCase("niiiiggggerrrr")]
    [TestCase("n i g g e r")]
    [TestCase("n.i.g-g_e r")]
    [TestCase("ni gger")]
    [TestCase("nìggér")]
    [TestCase("ni​gger")]
    [TestCase("nіgger")] // cyrillic i
    [TestCase("sandniggers")]
    [TestCase("you're a retard")]
    [TestCase("RETARDED")]
    [TestCase("motherfucker")]
    [TestCase("f u c k off")]
    [TestCase("sh1t")]
    [TestCase("bullshit")]
    [TestCase("stupid spic")]
    [TestCase("spics")]
    [TestCase("what a dick")]
    // the allowed entries above must not defuse the bare words
    [TestCase("stupid spic")]
    [TestCase("dyke")]
    // the rape family is wholeWord, so the bare words still have to be caught
    [TestCase("rape")]
    [TestCase("RAPING")]
    [TestCase("he is a rapist")]
    [TestCase("r a p e")]
    [TestCase("r4p3")]
    // these are inWord, so every form of them is caught
    [TestCase("molest")]
    [TestCase("he molested her")]
    [TestCase("molestation")]
    [TestCase("incest")]
    [TestCase("incestuous")]
    // "pedo" is wholeWord, plural included
    [TestCase("pedo")]
    [TestCase("pedos")]
    public void Blocks(string text)
    {
        Assert.That(Match(text), Is.Not.Null);
    }

    [TestCase("hello there, captain")]
    [TestCase("the crew from Niger and Nigeria")]
    [TestCase("fire retardant foam")]
    [TestCase("Scunthorpe")]
    [TestCase("shiitake mushrooms")]
    [TestCase("his hit was lethal")]
    [TestCase("that's suspicious")]
    [TestCase("Charles Dickens")]
    [TestCase("I am a u")]
    [TestCase("room 404, deck 3")]
    // "spic" picks up the s/es plural, so "spice" has to exempt it
    [TestCase("pass the spices please")]
    [TestCase("spiced ham")]
    // multi-word allowed entries have to survive the word gap
    [TestCase("Moustache (Van Dyke)")]
    [TestCase("he grew a van dyke")]
    // the gap is optional, so the run-together spelling is exempt too
    [TestCase("vandyke")]
    // "rape" only matches as a whole word, so these must stay clean
    [TestCase("a bunch of grapes")]
    [TestCase("drape the curtains")]
    [TestCase("scraping the hull")]
    [TestCase("go see the therapist")]
    [TestCase("trapeze artist")]
    // "pedo" must not fire inside these
    [TestCase("load the torpedo")]
    [TestCase("torpedoes away")]
    [TestCase("check the pedometer")]
    public void Allows(string text)
    {
        Assert.That(Match(text), Is.Null);
    }
}
