
VAR accused_damage = false
VAR accused_stain = false
VAR accused_accessories = false
VAR accused_receipt = false

'Arry: Hi. I'd like to return this Evil Blood Wizard costume. There's nothing wrong with it. My Halloween, on the other hand, was a disaster.
-> questions

=== questions ===
+ [Why are you returning this outfit?]
    'Arry: I got to the party and saw about fifteen people wearing the exact same thing. I wasn't about to spend the night looking like part of a group booking.
    -> questions

+ [When did you purchase this costume?]
    'Arry: A few days before Halloween. I spent an embarrassing amount of time choosing it, too. Clearly, so did everyone else.
    -> questions

+ [Have you ever worn this costume?]
    'Arry: Just tried it on. Saw everyone through the window and went straight home. Didn't even make it through the door. Bit pathetic, really.
    -> questions

+ [Accuse of anomaly]
    -> accusations

+ [Finish questioning]
    -> END

=== accusations ===
+ {not accused_damage} [Accuse of slight damage]
    ~ accused_damage = true
    'Arry: Damage? Where? I haven't even worn it outside. Unless looking at it in disappointment counts as damage.
    -> accusations

+ {not accused_stain} [Accuse of a stain]
    ~ accused_stain = true
    'Arry: It's fake blood. It's literally called the Evil Blood Wizard. What did you think the red stuff was supposed to be, ketchup?
    -> accusations

+ {not accused_accessories} [Accuse of missing accessories]
    ~ accused_accessories = true
    'Arry: The hat's right there. It's massive. I'm not exactly going to lose it down the back of the sofa.
    -> accusations

+ {not accused_receipt} [Accuse of a missing receipt]
    ~ accused_receipt = true
    'Arry: It's in the bag. I kept everything because I had a feeling this costume might turn out to be a mistake.
    -> accusations

+ [Back to questions]
    -> questions

=== refund_response ===
'Arry: Cheers. Next year I'm just going as myself. Apparently that's the only costume nobody else wants.
-> END

=== no_refund_response ===
'Arry: Brilliant. So I spent Halloween alone AND paid for the privilege. Thanks.
-> END
