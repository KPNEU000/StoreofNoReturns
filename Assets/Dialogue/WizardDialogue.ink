VAR accused_damage = false
VAR accused_stain = false
VAR accused_accessories = false
VAR accused_receipt = false

Wizard: Hello! I'd like to return my Evil Blood Wizard costume, please. It's completely unused, and I have my receipt right here.
-> questions

=== questions ===
+ [Why are you returning this outfit?]
    Wizard: I was going to a Halloween party, but my friend got there before me and said practically everyone was dressed as the Evil Blood Wizard! I wasn't about to show up looking like everyone else.
    -> questions

+ [When did you purchase this costume?]
    Wizard: Just before Halloween. I bought it specifically for that party, but I ended up staying home instead.
    -> questions

+ [Have you ever worn this costume?]
    Wizard: Not once! I didn't even make it to the party. It's in exactly the same condition as when I bought it.
    -> questions

+ [Accuse of anomaly]
    -> accusations

+ [Finish questioning]
    -> END


=== accusations ===
+ {not accused_damage} [Accuse of slight damage]
    ~ accused_damage = true
    Wizard: Damage? Where? These robes are exactly how they were when I bought them!
    -> accusations

+ {not accused_stain} [Accuse of a stain]
    ~ accused_stain = true
    Wizard: Those are FAKE bloodstains! It's called the Evil Blood Wizard! They're part of the design. You sold it to me like that!
    -> accusations

+ {not accused_accessories} [Accuse of missing accessories]
    ~ accused_accessories = true
    Wizard: The hat's right there! Everything that came with the costume is here.
    -> accusations

+ {not accused_receipt} [Accuse of a missing receipt]
    ~ accused_receipt = true
    Wizard: I literally handed you my receipt when I walked in. Have you even been listening?
    -> accusations

+ [Back to questions]
    -> questions


=== refund_response ===
Wizard: Oh, wonderful! Thank you very much. Perhaps next year I'll find a costume that's a little more original. Have a lovely evening!
-> END

=== no_refund_response ===
Wizard: You're refusing me? I haven't even worn the thing! The blood is painted on, for goodness' sake! I'll certainly remember this place.
-> END
