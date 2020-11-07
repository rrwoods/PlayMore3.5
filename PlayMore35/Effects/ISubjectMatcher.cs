namespace PlayMore35.Effects {
    public interface ISubjectMatcher<MatchType> : IJsonable {
        bool Matches(MatchType thing);
    }
}
