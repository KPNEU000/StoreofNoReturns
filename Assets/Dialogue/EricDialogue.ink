
VAR stain_cleared = false

VAR accused_damage = false
VAR accused_stain = false
VAR accused_accessories = false
VAR accused_receipt = false

VAR reputation_change = 0

Eric: Uh...hello there. I'm sorry to bother you on a night like tonight, but I had an issue with a costume I bought from you and I was wondering if I could...maybe...return it and get a refund?

-> questions


=== questions ===

+ [Why are you returning this outfit?]
    Eric: You see...my cat might've spilled something on it last night before my family and I went out. I think it was water or something, but it totally ruined our Halloween experience because I couldn't wear it out.
    -> questions

+ [When did you purchase this costume?]
    Eric: I ordered it, alongside costumes for my family, three weeks ago after we finished watching that new Extraordinaries movie that just released. They were supposed to be picked up last Saturday, but you were late to fulfil our order and I had to pick them up after my 8 hour shift on Tuesday!
    -> questions

+ [Have you ever worn this costume?]
    Eric: No. The...water...my cat spilled on this costume made it uncomfortable to wear. I had to just go trick-or-treating as myself. You can't have an Extraordinary family without Mr Extraordinary himself.
    -> questions

+ [Accuse of anomaly]
    -> accusations

+ [Finish questioning]
    -> END


=== accusations ===

+ {not accused_damage} [Accuse of slightly damaged costume]
    ~ accused_damage = true
    ~ reputation_change = reputation_change - 10
    Eric: What are you talking about, there are no rips or tears anywhere on this costume, are you calling me a liar?!?
    -> questions

+ {not accused_stain} [Accuse of stained costume]
    ~ accused_stain = true
    ~ stain_cleared = true
    Eric: Yeah, that might be a pee stain that my cat left on the costume or a vase that he tipped over on the counter but I promise that it was an accident and I will be giving Marshall no more treats this month for his behaviour.
    -> questions

+ {not accused_accessories} [Accuse of missing accessories]
    ~ accused_accessories = true
    ~ reputation_change = reputation_change - 10
    Eric: I brought all the accessories that were given to me back with the costume as instructed, I read your store policies!
    -> questions

+ {not accused_receipt} [Accuse of missing receipt]
    ~ accused_receipt = true
    ~ reputation_change = reputation_change - 10
    Eric: I just gave you the receipt, are you high or something, damn they just hire anyone now.
    -> questions

+ [Back to questions]
    -> questions


=== refund_response ===

Eric: Thank you! You won't forget what you did for me. Little Marshal is going to get a load of lip when I get home. And don't worry...I'll be sure to leave a good review on that Internet Navigator site or whatever it's called! Goodbye!

{
    - stain_cleared:
        ~ reputation_change = reputation_change + 10
    - else:
        ~ reputation_change = reputation_change - 5
}

-> END


=== no_refund_response ===

~ reputation_change = reputation_change - 10

Eric: Are you kidding me! You can't blame me for something Marshal did! Some of us are more liable to accidents than others! Unbelievable. Your ratings are gonna hear it from me when I get home!

-> END
