namespace PlayMore35.Effects {
    public interface ISubjectClass<MatchType> : IJsonable {
        bool Matches(MatchType thing);
    }
}
