using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PokerCrosswords
{
	public enum Rank
	{
		Two = 2,
		Three = 3,
		Four = 4,
		Five = 5,
		Six = 6,
		Seven = 7,
		Eight = 8,
		Nine = 9,
		Ten = 10,
		Jack = 11,
		Queen = 12,
		King = 13,
		Ace = 14
	}

	public enum Suit
	{
		Hearts,
		Spades,
		Diamonds,
		Clubs
	}

	public class Card
	{
		public Suit Suit { get; }
		public Rank Rank { get; }
		
		public Card(Suit suit, Rank rank)
		{
			Suit = suit;
			Rank = rank;
		}

		public override string ToString()
		{
			return (Rank <= Rank.Ten ? ((int)Rank).ToString() : Rank.ToString().Substring(0, 1))
				+ Suit.ToString().ToLower()[0];
		}
	}
	
	public enum HandStrength
	{
		Nothing = 0,
		Pair = 1,
		TwoPair = 2,
		ThreeOfAKind = 3,
		Straight = 4,
		Flush = 5,
		FullHouse = 6,
		FourOfAKind = 7,
		StraightFlush = 8
	}

	public class Hand : IComparable<Hand>
	{
		public IReadOnlyList<Card> Cards { get; }
		private Hand(IReadOnlyList<Card> cards)
		{
			if (cards.Count != 5)
				throw new ArgumentException($"A hand requires five cards but you passed {cards.Count}", nameof(cards));
			Cards = cards;
		}

		public Hand(params Card[] cards) : this(cards.ToList()) { }
		public Hand(IEnumerable<Card> cards) : this(cards.ToList()) { }

		public override string ToString()
		{
			return string.Join("", Cards);
		}

		private static int CompareKickers(Rank x, Rank y)
		{
			if (x > y)
				return 1;
			return x == y ? 0 : -1;
		}

		private static int CompareRanks(ICollection<Rank> x, ICollection<Rank> y)
		{
			var xMax = x.Max();
			var yMax = y.Max();
			if (xMax > yMax) return 1;
			if (xMax < yMax) return -1;
			x.Remove(xMax);
			y.Remove(yMax);
			return CompareRanks(x, y);
		}

		public HandStrength GetHandStrength() => ReduceToRanks(out var unused);

		private HandStrength ReduceToRanks(out Rank[] simplifiedHand)
		{
			var isFlush = Cards.All(c => c.Suit == Cards[0].Suit);
			var ranks = Cards.Select(c => c.Rank).OrderByDescending(r => r).ToArray();
			var isStraight = ranks.Skip(1)
				.Select((r, i) => (int)ranks[i] == (int)r + 1 || ranks[i] == Rank.Two && r == Rank.Ace)
				.All(_ => _);

			if (isStraight) {
				simplifiedHand = new[] { ranks.Max() };
				return isFlush ? HandStrength.StraightFlush : HandStrength.Straight;
			}
			
			if (isFlush) {
				simplifiedHand = ranks;
				return HandStrength.Flush;
			}

			var ranksByCount = ranks
				.GroupBy(r => r)
				.GroupBy(g => g.Count())
				.ToDictionary(g => g.Key, g => g.Select(rg => rg.Key).ToList());
			
			if (ranksByCount.TryGetValue(4, out var quads)) {
				simplifiedHand = new[] { quads.Single(), ranksByCount[1].Single() };
				return HandStrength.FourOfAKind;
			}

			var hasPairs = ranksByCount.TryGetValue(2, out var pairs);
			if (ranksByCount.TryGetValue(3, out var trips)) {
				if (hasPairs) {
					simplifiedHand = new[] { trips.Single(), pairs.Single() };
					return HandStrength.FullHouse;
				}

				simplifiedHand = trips.Concat(ranksByCount[1].OrderByDescending(r => r)).ToArray();
				return HandStrength.ThreeOfAKind;
			}

			if (hasPairs) {
				simplifiedHand = pairs.OrderByDescending(r => r).Concat(ranksByCount[1].OrderByDescending(r => r)).ToArray();
				return pairs.Count == 2 ? HandStrength.TwoPair : HandStrength.Pair;
			}

			simplifiedHand = ranks;
			return HandStrength.Nothing;
		}

		public int CompareTo(Hand otherHand)
		{
			var strength = ReduceToRanks(out var simplified);
			var otherStrength = otherHand.ReduceToRanks(out var otherSimplified);
			if (strength > otherStrength) return 1;
			if (strength < otherStrength) return -1;
			return CompareRanks(simplified, otherSimplified);
		}

		public static bool operator >(Hand x, Hand y) => x.CompareTo(y) > 0;
		public static bool operator <(Hand x, Hand y) => x.CompareTo(y) < 0;
		public static bool operator ==(Hand x, Hand y) => x.CompareTo(y) == 0;
		public static bool operator !=(Hand x, Hand y) => x.CompareTo(y) != 0;
	}
	
	public class Deck
	{
		private Queue<Card> Cards { get; set; }

		public Deck()
		{
			var suits = Enum.GetValues(typeof(Suit)).Cast<Suit>().ToList();
			var ranks = Enum.GetValues(typeof(Rank)).Cast<Rank>().ToList();
			Cards = new Queue<Card>(suits.SelectMany(s => ranks.Select(r => new Card(s, r))));
			Shuffle();
		}

		private static readonly Lazy<Random> Random = new Lazy<Random>(() => new Random());
		
		public void Shuffle() => Cards = new Queue<Card>(Cards.OrderBy(_ => Random.Value.Next()));

		public bool Rig(HandStrength nextDesiredHand)
		{
			var cards = Cards.ToList();
			for (var a = 0; a < cards.Count; a++) {
				for (var b = a + 1; b < cards.Count; b++) {
					for (var c = b + 1; c < cards.Count; c++) {
						for (var d = c + 1; d < cards.Count; d++) {
							for (var e = d + 1; e < cards.Count; e++) {
								var hand = new Hand(cards[a], cards[b], cards[c], cards[d], cards[e]);
								if (hand.GetHandStrength() == nextDesiredHand) {
									Cards = new Queue<Card>(hand.Cards.Concat(Cards.Except(hand.Cards).OrderBy(_ => Random.Value.Next())));
									return true;
								}
							}
						}
					}
				}
			}

			return false;
		}

		public Card DrawOneCard() => Cards.Count > 0 ? Cards.Dequeue() : null;
	}
}