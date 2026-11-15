using OpenMyo.Core.Text;

namespace OpenMyo.Core.Tests.Text;

public sealed class TitleReviewTests
{
    [Fact]
    public void Evaluate_CleanOrderedTitles_HasNoSuggestions()
    {
        var review = TitleReview.Evaluate(["Twinkle Twinkle", "Baa Baa Black Sheep"]);

        Assert.False(review.HasSuggestions);
    }

    [Fact]
    public void Evaluate_MessyFileNames_CountsTitlesToTidyWithExample()
    {
        var review = TitleReview.Evaluate(["01 - intro.mp3", "Second Song"]);

        Assert.Equal(1, review.TidyCount);
        Assert.StartsWith("01 - intro.mp3 → ", review.TidyExample, StringComparison.Ordinal);
    }

    [Fact]
    public void Evaluate_NumberedTitlesOutOfOrder_IsOutOfOrder()
    {
        var review = TitleReview.Evaluate(["Episode 2", "Episode 10", "Episode 1"]);

        Assert.True(review.IsOutOfOrder);
    }

    [Fact]
    public void Evaluate_UnnumberedTitlesInAnyOrder_IsNotOutOfOrder()
    {
        var review = TitleReview.Evaluate(["Zebra Song", "Apple Song"]);

        Assert.False(review.IsOutOfOrder);
    }
}
