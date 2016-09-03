/*
AdamMil.Mathematics is a library that provides some useful mathematics classes
for the .NET framework.

http://www.adammil.net/
Copyright (C) 2007-2016 Adam Milazzo

This program is free software; you can redistribute it and/or
modify it under the terms of the GNU General Public License
as published by the Free Software Foundation; either version 2
of the License, or (at your option) any later version.
This program is distributed in the hope that it will be useful,
but WITHOUT ANY WARRANTY; without even the implied warranty of
MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
GNU General Public License for more details.
You should have received a copy of the GNU General Public License
along with this program; if not, write to the Free Software
Foundation, Inc., 59 Temple Place - Suite 330, Boston, MA  02111-1307, USA.
*/
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using AdamMil.Utilities;

namespace AdamMil.Mathematics
{

static class NumberFormat
{
  public static decimal DigitsToDecimal(byte[] digits, int decimalPlace, bool negative)
  {
    if(decimalPlace <= -29) return 0m; // if there are at least 29 leading fractional zeros, it must round to zero
    else if(decimalPlace > 28) throw new OverflowException(); // if it's at least 10^29 in magnitude, there's definitely an overflow

    // round to at most 28 digits by pretending that all digits are in the fraction (start == 0)
    int start = 0, dummy = 0, digitCount = NumberFormat.RoundDigits(digits, ref start, false, ref dummy, 28);
    decimalPlace += start; // take into account any shifting in the decimal place done by RoundDigits
    Integer mantissa = Integer.ParseDigits(digits, digitCount);
    if(decimalPlace > 28 || mantissa.BitLength > 96) throw new OverflowException();

    int ilo=0, imid=0, ihi=0;
    if(!mantissa.IsZero)
    {
      uint[] bits = mantissa.GetBits();
      ilo = (int)bits[0];
      if(mantissa.BitLength > 32)
      {
        imid = (int)bits[1];
        if(mantissa.BitLength > 64) ihi = (int)bits[2];
      }
    }
    return new decimal(ilo, imid, ihi, negative, (byte)(digitCount - decimalPlace));
  }

  public static string FormatNumber(byte[] digits, int decimalPlace, bool isNegative, NumberFormatInfo nums, char formatType,
                                    int desiredPrecision, int defaultPrecision, bool capitalize)
  {
    int leadingZeros = 0;
    bool exponentialFormat = false, groupDigits = false;

    // set 'exponentialFormat' and 'groupDigits' based on the formatting options
    switch(formatType)
    {
      case 'C': // currency
        groupDigits = true;
        if(desiredPrecision == -1) desiredPrecision = nums.CurrencyDecimalDigits;
        break;
      case 'D': // integer
        // the precision actually specifies the number of digits, so we may need some leading zeros
        if(desiredPrecision > digits.Length) leadingZeros = desiredPrecision - digits.Length;
        desiredPrecision = 0; // no fractional digits with the integer format
        break;
      case 'E': // exponential notation with the typical precision by default
        exponentialFormat = true;
        if(desiredPrecision == -1) desiredPrecision = defaultPrecision;
        break;
      case 'F': // number with ungrouped digits and culture precision by default
        if(desiredPrecision == -1) desiredPrecision = nums.NumberDecimalDigits;
        break;
      case 'G': // E or F, whichever is shorter, but using the typical precision by default in either case
        if(desiredPrecision == -1) desiredPrecision = defaultPrecision;
        exponentialFormat = ComputeExponentialLength(digits, decimalPlace, formatType, desiredPrecision, nums) <
                            ComputeDecimalLength(digits, decimalPlace, formatType, desiredPrecision, nums);
        break;
      case 'N': // a number using normal cultural conventions, including culture precision
        groupDigits = true;
        if(desiredPrecision == -1) desiredPrecision = nums.NumberDecimalDigits;
        break;
      case 'P': // a percentage, using cultural conventions, including culture precision
        groupDigits = true;
        if(desiredPrecision == -1) desiredPrecision = nums.PercentDecimalDigits;
        decimalPlace += 2; // multiply the number by 100 by shifting the decimal point
        break;
      case 'R': // a round-trip format. either E or F, whichever is shorter, but always maximum precision
        exponentialFormat = ComputeExponentialLength(digits, decimalPlace, formatType, -1, nums) <
                            ComputeDecimalLength(digits, decimalPlace, formatType, -1, nums);
        desiredPrecision = -1; // don't limit precision in round-trip format
        break;
    }

    // if exponential format is used, calculate the exponent and put the decimal point after the first digit, which is assumed to be
    // non-zero if the number itself is non-zero
    int exponent = 0;
    if(exponentialFormat)
    {
      exponent     = decimalPlace - 1;
      decimalPlace = 1;
      if(desiredPrecision == -1) // since we moved the decimal point, we may have trailing zeros to trim
      {
        desiredPrecision = digits.Length-1;
        while(desiredPrecision > 0 && digits[desiredPrecision] == 0) desiredPrecision--;
        if(desiredPrecision == digits.Length-1) desiredPrecision = -1; // don't round pointlessly if we didn't trim any digits
      }
    }

    // if a precision specifier was given, round the fraction off to that precision if necessary
    int digitCount = desiredPrecision == -1 ? digits.Length : // use all digits if we're not rounding
      RoundDigits(digits, ref decimalPlace, exponentialFormat, ref exponent, desiredPrecision); // otherwise, subtract some for rounding

    // if the number is negative zero (potentially due to rounding) and we're not using round-trip format, then avoid rendering
    // the negative zero
    if(isNegative && digitCount == 1 && digits[0] == 0 && formatType != 'R') isNegative = false;

    // now render the number along with any symbols (e.g. dollar sign, percent, etc.)
    StringBuilder sb = new StringBuilder(digitCount + 16);
    AddLeadingNumberSymbols(sb, isNegative, nums, formatType);
    sb.Append('0', leadingZeros);
    AddNumber(sb, digits, digitCount, decimalPlace, exponentialFormat, exponent, formatType, nums, capitalize, groupDigits);
    AddTrailingNumberSymbols(sb, isNegative, nums, formatType);
    return sb.ToString();
  }

  public static int GetDefaultPrecision(NumberFormatInfo nums, char formatType)
  {
    switch(formatType)
    {
      case 'C': return nums.CurrencyDecimalDigits;
      case 'D': return 0;
      case 'F': case 'N': return nums.NumberDecimalDigits;
      case 'P': return nums.PercentDecimalDigits;
      default: return -1; // unspecified or unknown for this format
    }
  }

  public static bool ParseFormatString(string format, char defaultType, out char formatType, out int desiredPrecision, out bool capitalize)
  {
    desiredPrecision = -1;

    bool badFormat = false;
    if(string.IsNullOrEmpty(format))
    {
      formatType = char.ToUpperInvariant(defaultType);
      capitalize = !char.IsLower(defaultType);
    }
    else
    {
      formatType = char.ToUpperInvariant(format[0]);
      capitalize = formatType == format[0];
      if(formatType == 'D' || formatType == 'F' || formatType == 'E' || formatType == 'G' || formatType == 'R' || formatType == 'X' ||
         formatType == 'N' || formatType == 'P' || formatType == 'C')
      {
        badFormat = format.Length > 1 &&
                    (!InvariantCultureUtility.TryParseExact(format, 1, format.Length-1, out desiredPrecision) || desiredPrecision < 0);
      }
      else
      {
        badFormat = true;
      }
    }
    return !badFormat;
  }

  public static byte[] ParseSignificantDigits(string str, int start, int end, NumberStyles style, NumberFormatInfo nums,
                                              out int digitCount, out int exponent, out bool negative)
  {
    digitCount = 0;
    exponent   = 0;
    negative   = false;

    // keep track of various symbols we've seen
    bool currency = false, negativeSign = false, parens = false, percent = false, permille = false, positiveSign = false;
    bool sawUnknownChar = false;

    // scan the leading symbols until we find the first digit or unrecognized character
    char negativeChar = GetFirstChar(nums.NegativeSign), currencyChar = GetFirstChar(nums.CurrencySymbol);
    char percentChar = GetFirstChar(nums.PercentSymbol), permilleChar = GetFirstChar(nums.PerMilleSymbol);
    char positiveChar = GetFirstChar(nums.PositiveSign);
    for(; start < end; start++)
    {
      char c = str[start];
      if(c >= '0' && c <= '9')
      {
        break; // stop scanning if we find the first digit
      }
      else if(char.IsWhiteSpace(c))
      {
        continue; // skip over whitespace
      }
      else if(c == negativeChar && str.StartsAt(start, nums.NegativeSign))
      {
        if((negativeSign | positiveSign) || (style & NumberStyles.AllowLeadingSign) == 0) return null;
        negative = negativeSign = true;
        start += nums.NegativeSign.Length - 1;
      }
      else if(c == positiveChar && str.StartsAt(start, nums.PositiveSign))
      {
        if((negative | positiveSign) || (style & NumberStyles.AllowLeadingSign) == 0) return null;
        positiveSign = true;
        start += nums.PositiveSign.Length - 1;
      }
      else if(c == '(') // parentheses indicate negative numbers
      {
        if((parens | positiveSign) || (style & NumberStyles.AllowParentheses) == 0) return null;
        negative = parens = true;
      }
      else if(c == currencyChar && str.StartsAt(start, nums.CurrencySymbol))
      {
        if((currency | percent | permille) || (style & NumberStyles.AllowCurrencySymbol) == 0) return null;
        currency = true;
        start += nums.CurrencySymbol.Length - 1;
      }
      else if(c == percentChar && str.StartsAt(start, nums.PercentSymbol))
      {
        if(currency | percent | permille) return null;
        percent = true;
        end -= nums.PercentSymbol.Length - 1;
      }
      else if(c == permilleChar && str.StartsAt(start, nums.PerMilleSymbol))
      {
        if(currency | percent | permille) return null;
        permille = true;
        end -= nums.PerMilleSymbol.Length - 1;
      }
      else // otherwise, if the character is unknown, stop scanning
      {
        // we should return null if we see an unknown character, but since the number could start with a decimal point and the
        // character used for the decimal point depends on the type of string (currency, percent, etc.), and the type of string
        // may not be determined yet, we don't know whether the unknown character is truly unknown. we'll check it later
        sawUnknownChar = true;
        break;
      }
    }

    // now scan the trailing symbols until we find the last digit or unrecognized character
    negativeChar = GetLastChar(nums.NegativeSign);
    currencyChar = GetLastChar(nums.CurrencySymbol);
    percentChar  = GetLastChar(nums.PercentSymbol);
    permilleChar = GetLastChar(nums.PerMilleSymbol);
    positiveChar = GetLastChar(nums.PositiveSign);
    while(start < end)
    {
      char c = str[end-1];
      if(c >= '0' && c <= '9') break;
      end--;

      if(char.IsWhiteSpace(c))
      {
        continue;
      }
      else if(c == ')')
      {
        if(!parens) return null;
      }
      else if(c == negativeChar && str.EndsAt(end, nums.NegativeSign))
      {
        if(negativeSign || (style & NumberStyles.AllowTrailingSign) == 0) return null;
        negative = negativeSign = true;
        end -= nums.NegativeSign.Length - 1;
      }
      else if(c == positiveChar && str.EndsAt(end, nums.PositiveSign))
      {
        if((negativeSign | positiveSign) || (style & NumberStyles.AllowTrailingSign) == 0) return null;
        positiveSign = true;
        end -= nums.PositiveSign.Length - 1;
      }
      else if(c == currencyChar && str.EndsAt(end, nums.CurrencySymbol))
      {
        if((currency | percent | permille) || (style & NumberStyles.AllowCurrencySymbol) == 0) return null;
        currency = true;
        end -= nums.CurrencySymbol.Length - 1;
      }
      else if(c == percentChar && str.EndsAt(end, nums.PercentSymbol))
      {
        if(currency | percent | permille) return null;
        percent = true;
        end -= nums.PercentSymbol.Length - 1;
      }
      else if(c == permilleChar && str.EndsAt(end, nums.PerMilleSymbol))
      {
        if(currency | percent | permille) return null;
        permille = true;
        end -= nums.PerMilleSymbol.Length - 1;
      }
      else // otherwise, if the character is unknown, stop scanning
      {
        // we should return null if we see an unknown character, but since the number could end with a decimal point and the character
        // used for the decimal point depends on the type of string (currency, percent, etc.), and the type of string may not have been
        // determined when we started scanning, we don't know whether the unknown character is illegal or is just a decimal, so we'll
        // defer that check until the next part
        sawUnknownChar = true;
        end++;
        break;
      }
    }

    // now that we know the type of string, we can determine the string used for decimal points
    string decimalStr = currency ? nums.CurrencyDecimalSeparator :
                        percent | permille ? nums.PercentDecimalSeparator : nums.NumberDecimalSeparator;
    char decimalChar = GetFirstChar(decimalStr);

    // if we saw a character that might have been illegal, check whether it was illegal or just a decimal point
    if(sawUnknownChar && start < end)
    {
      char c = str[start]; // check for an illegal character at the start
      if((c < '0' || c > '9') && (c != decimalChar || !str.StartsAt(start, decimalStr))) return null;

      if(start < end-1) // if the length is greater than one, check for an illegal character at the end too
      {
        c = str[end-1];
        if((c < '0' || c > '9') && (c != decimalChar || !str.EndsAt(end-1, decimalStr))) return null;
      }
    }

    // now that we've identified the region containing the digits, read them
    string groupStr = currency ? nums.CurrencyGroupSeparator : // figure out the type of digit group separator
                      percent | permille ? nums.PercentGroupSeparator : nums.NumberGroupSeparator;
    int decimalPlace = 0; // the location of the decimal point. it may be outside the array, since we exclude leading and trailing zeros
    char groupChar = GetFirstChar(groupStr);
    bool lastWasDigit = false, sawDecimal = false, sawDigit = false, sawNonzeroDigit = false;

    byte[] digits = new byte[end-start];
    for(; start < end; start++)
    {
      char c = str[start];
      if(c >= '0' && c <= '9')
      {
        sawDigit = lastWasDigit = true;
        if(c != '0')
        {
          sawNonzeroDigit = true;
        }
        else if(!sawNonzeroDigit) // if it's a leading zero...
        {
          if(sawDecimal) decimalPlace--; // if we're in the fraction, move the decimal place
          continue; // and skip the leading zero
        }
        digits[digitCount++] = (byte)(c - '0'); // save the character
        if(!sawDecimal) decimalPlace++; // if we're not in the fraction, move the decimal place
      }
      else // if this character isn't a digit...
      {
        // allow spaces in the whole part if we allow digit grouping. (some software groups digits with spaces)
        if(c == ' ' && !sawDecimal && (style & NumberStyles.AllowThousands) != 0)
        {
          continue; // don't clear lastWasDigit when skipping over spaces. (pretend spaces simply don't exist.)
        }
        else if(c == decimalChar && str.StartsAt(start, decimalStr))
        {
          if(sawDecimal || (style & NumberStyles.AllowDecimalPoint) == 0) return null;
          sawDecimal = true;
        }
        else if(char.ToUpperInvariant(c) == 'E') // if the number was given in exponential notation...
        {
          if(!sawDigit || !InvariantCultureUtility.TryParseExact(str, start+1, end-(start+1), out exponent)) return null;
          break;
        }
        else if(c == groupChar && str.StartsAt(start, groupStr))
        {
          if(!lastWasDigit || (style & NumberStyles.AllowThousands) == 0) return null; // only allow group characters after digits
        }
        else
        {
          return null;
        }
        lastWasDigit = false;
      }
    }

    // there must be at least one digit in the string (but not necessarily in the digits array)
    if(!sawDigit) return null;

    // if we only saw leading zeros and skipped over them, add one zero back so we can have at least one digit
    if(digitCount == 0) digitCount = decimalPlace = 1;

    // trim trailing zeros from the fraction
    while(digitCount > decimalPlace && digits[digitCount-1] == 0) digitCount--;

    // update the exponent based on the decimal point and whether any percent or permille symbols were found
    exponent -= digitCount - decimalPlace;
    if(percent) exponent -= 2; // percent implies division by 100
    else if(permille) exponent -= 3; // permille implies division by 1000

    return digits;
  }

  public static int RoundDigits(byte[] digits, ref int decimalPlace, bool exponentialFormat, ref int exponent, int desiredPrecision)
  {
    int digitCount = digits.Length; // ignore trailing zeros
    while(digitCount-1 > 0 && digits[digitCount-1] == 0) digitCount--;
    int fractionDigits = digitCount - decimalPlace, roundOff = fractionDigits - desiredPrecision;
    if(roundOff > 0) // if rounding is needed...
    {
      if(roundOff <= digitCount)
      {
        int i, roundIndex = digitCount - roundOff;
        bool roundUp = digits[roundIndex] > 5;
        if(digits[roundIndex] == 5) // if we're not sure if we need to round up...
        {
          for(i=roundIndex+1; i<digitCount; i++)
          {
            if(digits[i] != 0) { roundUp = true; break; }
          }
          // if all the remaining digits were zeros, round to even
          if(i == digitCount) roundUp = roundIndex == 0 || (digits[roundIndex-1] & 1) == 1;
        }

        digitCount -= roundOff;
        if(roundUp) // if we need to round up, increment the previous digit and all prior digits equal to nine
        {
          i = roundIndex - 1;
          while(i >= 0)
          {
            if(++digits[i] != 10) break; // if adding one didn't go from 9 to 10, then we're done
            else digits[i--] = 0; // otherwise, set that digit to 0 and move to the previous digit
          }
          if(i < 0) // if all the prior digits were 9 (or there were no prior digits)...
          {
            digits[0] = 1; // stick a 1 into the digit array
            // since it actually represents the previous digit now, change the exponent or decimal place
            if(exponentialFormat) exponent++;
            else decimalPlace++;
            if(digitCount == 0) digitCount++;
          }
        }

        // after rounding, we may have trailing zeros in the fraction. remove them
        for(i=digitCount-1; i >= 0 && digits[i] == 0; i--) digitCount--;
        roundOff = 0; // no digits left to round. (avoid going into the 'if' block below)
      }

      if(roundOff > digitCount || digitCount == 0) // if there are more leading fractional zeros than desired digits...
      {
        digits[0]    = 0; // then the whole number should round to zero
        decimalPlace = digitCount = 1; // just set decimalPlace and not exponent because this can't happen with exponential notation
      }
    }

    return digitCount;
  }

  static void AddLeadingNumberSymbols(StringBuilder sb, bool isNegative, NumberFormatInfo nums, char formatType)
  {
    if(isNegative)
    {
      if(formatType == 'C') // currency
      {
        switch(nums.CurrencyNegativePattern)
        {
          case 0: sb.Append('(').Append(nums.CurrencySymbol); break;                            // ($n)
          case 1: sb.Append(nums.NegativeSign).Append(nums.CurrencySymbol); break;              // -$n
          case 2: sb.Append(nums.CurrencySymbol).Append(nums.NegativeSign); break;              // $-n
          case 3: sb.Append(nums.CurrencySymbol); break;                                        // $n-
          case 4: case 15: sb.Append('('); break;                                               // (n$) or (n $)
          case 5: case 8: sb.Append(nums.NegativeSign); break;                                  // -n$ or -n $
          case 9: sb.Append(nums.NegativeSign).Append(nums.CurrencySymbol).Append(' '); break;  // -$ n
          case 11: sb.Append(nums.CurrencySymbol).Append(' '); break;                           // $ n-
          case 12: sb.Append(nums.CurrencySymbol).Append(' ').Append(nums.NegativeSign); break; // $ -n
          case 14: sb.Append('(').Append(nums.CurrencySymbol).Append(' '); break;               // ($ n)
        }
      }
      else if(formatType == 'P') // percent
      {
        switch(nums.PercentNegativePattern)
        {
          case 0: case 1: sb.Append(nums.NegativeSign); break;                                 // -n % or -n%
          case 2: sb.Append(nums.NegativeSign).Append(nums.PercentSymbol); break;              // -%n
          case 3: sb.Append(nums.PercentSymbol).Append(nums.NegativeSign); break;              // %-n
          case 4: sb.Append(nums.PercentSymbol); break;                                        // %n-
          case 7: sb.Append(nums.NegativeSign).Append(nums.PercentSymbol).Append(' '); break;  // -% n
          case 9: sb.Append(nums.PercentSymbol).Append(' '); break;                            // % n-
          case 10: sb.Append(nums.PercentSymbol).Append(' ').Append(nums.NegativeSign); break; // % -n
        }
      }
      else
      {
        switch(nums.NumberNegativePattern)
        {
          case 0: sb.Append('('); break;                           // (n)
          case 1: sb.Append(nums.NegativeSign); break;             // -n
          case 2: sb.Append(nums.NegativeSign).Append(' '); break; // - n
        }
      }
    }
    else // the number is positive
    {
      if(formatType == 'C') // currency
      {
        if(nums.CurrencyPositivePattern == 0) sb.Append(nums.CurrencySymbol); // $n
        else if(nums.CurrencyPositivePattern == 2) sb.Append(nums.CurrencySymbol).Append(' '); // $ n
      }
      else if(formatType == 'P') // percent
      {
        if(nums.PercentPositivePattern == 2) sb.Append(nums.PercentSymbol); // %n
        else if(nums.PercentPositivePattern == 3) sb.Append(nums.PercentSymbol).Append(' '); // % n
      }
    }
  }

  static void AddNumber(StringBuilder sb, byte[] digits, int digitCount, int decimalPlace, bool exponentialFormat, int exponent,
                        char formatType, NumberFormatInfo nums, bool capitalize, bool groupDigits)
  {
    // skip leading zeros besides the first
    int start = 0;
    while(start < digits.Length && start < decimalPlace-1 && digits[start] == 0) start++;

    // if grouping is enabled and there are any whole digits, break the digits up into groups
    string groupSeparator = null;
    List<int> groupIndexes = null; // indexes of digits within the whole part before which group separators should be inserted
    if(groupDigits && decimalPlace > 0)
    {
      int[] groupSizes;
      if(formatType == 'C') // currency
      {
        groupSeparator = nums.CurrencyGroupSeparator;
        groupSizes     = nums.CurrencyGroupSizes;
      }
      else if(formatType == 'P') // percent
      {
        groupSeparator = nums.PercentGroupSeparator;
        groupSizes     = nums.PercentGroupSizes;
      }
      else
      {
        groupSeparator = nums.NumberGroupSeparator;
        groupSizes     = nums.NumberGroupSizes;
      }

      if(groupSizes.Length == 0 || groupSizes[0] == 0) // if there is no grouping in this culture...
      {
        groupDigits = false; // don't group
      }
      else
      {
        groupIndexes = new List<int>();
        int i, count = start; // the number of digits examined so far
        // go through all groups until we reach the last one or until we run out of digits to group
        for(i=0; i<groupSizes.Length-1 && decimalPlace-count > groupSizes[i]; i++)
        {
          groupIndexes.Add(decimalPlace - count - groupSizes[i]); // add an index calculated from the right side
          count += groupSizes[i];
        }
        if(i == groupSizes.Length-1) // if we got through all groups but the last one...
        {
          int lastGroupSize = groupSizes[groupSizes.Length-1]; // get the last group, which is repeated
          if(lastGroupSize != 0) // if the last group size isn't zero (meaning 'stop grouping'), group the remaining digits
          {
            for(; decimalPlace-count > lastGroupSize; count += lastGroupSize) groupIndexes.Add(decimalPlace-count-lastGroupSize);
          }
        } 
        groupIndexes.Reverse(); // the indexes were added starting from the right, but the string is rendered from the left, so reverse
      }
    }

    // get the decimal string to use, and start rendering the number
    string decimalSymbol = formatType == 'C' ? nums.CurrencyDecimalSeparator :
                           formatType == 'P' ? nums.PercentDecimalSeparator  : nums.NumberDecimalSeparator;
    // add leading zeros, if any
    if(decimalPlace <= 0) // if the number is less than 1...
    {
      sb.Append('0'); // add the leading zero in the one's place
      if(decimalPlace < 0) sb.Append(decimalSymbol).Append('0', -decimalPlace); // add leading zeros in the fraction, if any
    }

    // now add the significant digits
    int groupIndex = 0;
    for(int i=start; i<digitCount; i++)
    {
      if(i == decimalPlace)
      {
        sb.Append(decimalSymbol);
      }
      else if(groupDigits && groupIndex < groupIndexes.Count && i == groupIndexes[groupIndex])
      {
        sb.Append(groupSeparator);
        groupIndex++;
      }
      sb.Append((char)('0' + digits[i]));
    }

    // now add trailing zeros, if any
    if(decimalPlace > digitCount)
    {
      if(!groupDigits)
      {
        sb.Append('0', decimalPlace-digitCount);
      }
      else
      {
        for(int i=digitCount; i<decimalPlace; i++)
        {
          if(groupDigits && groupIndex < groupIndexes.Count && i == groupIndexes[groupIndex])
          {
            sb.Append(groupSeparator);
            groupIndex++;
          }
          sb.Append('0');
        }
      }
    }

    // add the exponent if we're using exponential notation
    if(exponentialFormat)
    {
      sb.Append(capitalize ? 'E' : 'e');
      if(exponent >= 0) sb.Append('+');
      sb.Append(exponent.ToStringInvariant());
    }
  }

  static void AddTrailingNumberSymbols(StringBuilder sb, bool isNegative, NumberFormatInfo nums, char formatType)
  {
    if(isNegative)
    {
      if(formatType == 'C')
      {
        switch(nums.CurrencyNegativePattern)
        {
          case 0: case 14: sb.Append(')'); break;                                               // ($n) or ($ n)
          case 3: case 11: sb.Append(nums.NegativeSign); break;                                 // $n- or $ n-
          case 4: sb.Append(nums.CurrencySymbol).Append(')'); break;                            // (n$)
          case 5: sb.Append(nums.CurrencySymbol); break;                                        // -n$
          case 6: sb.Append(nums.NegativeSign).Append(nums.CurrencySymbol); break;              // n-$
          case 7: sb.Append(nums.CurrencySymbol).Append(nums.NegativeSign); break;              // n$-
          case 8: sb.Append(' ').Append(nums.CurrencySymbol); break;                            // -n $
          case 10: sb.Append(' ').Append(nums.CurrencySymbol).Append(nums.NegativeSign); break; // n $-
          case 13: sb.Append(nums.NegativeSign).Append(' ').Append(nums.CurrencySymbol); break; // n- $
          case 15: sb.Append(' ').Append(nums.CurrencySymbol).Append(')'); break;               // (n $)
        }
      }
      else if(formatType == 'P')
      {
        switch(nums.PercentNegativePattern)
        {
          case 0: sb.Append(' ').Append(nums.PercentSymbol); break;                            // -n %
          case 1: sb.Append(nums.PercentSymbol); break;                                        // -n%
          case 4: case 9: sb.Append(nums.NegativeSign); break;                                 // %n- or % n-
          case 5: sb.Append(nums.NegativeSign).Append(nums.PercentSymbol); break;              // n-%
          case 6: sb.Append(nums.PercentSymbol).Append(nums.NegativeSign); break;              // n%-
          case 8: sb.Append(' ').Append(nums.PercentSymbol).Append(nums.NegativeSign); break;  // n %-
          case 11: sb.Append(nums.NegativeSign).Append(' ').Append(nums.PercentSymbol); break; // n- %
        }
      }
      else
      {
        switch(nums.NumberNegativePattern)
        {
          case 0: sb.Append(')'); break;                           // (n)
          case 3: sb.Append(nums.NegativeSign); break;             // n-
          case 4: sb.Append(' ').Append(nums.NegativeSign); break; // n -
        }
      }
    }
    else // the number is positive
    {
      if(formatType == 'C')
      {
        if(nums.CurrencyPositivePattern == 1) sb.Append(nums.CurrencySymbol); // n$
        else if(nums.CurrencyPositivePattern == 3) sb.Append(' ').Append(nums.CurrencySymbol); // $ n
      }
      else if(formatType == 'P')
      {
        if(nums.PercentPositivePattern == 0) sb.Append(' ').Append(nums.PercentSymbol); // n %
        else if(nums.PercentPositivePattern == 1) sb.Append(nums.PercentSymbol); // n%
      }
    }
  }

  /// <summary>Computes the rough length of the number if it's rendered in decimal format to the given precision.</summary>
  static int ComputeDecimalLength(byte[] digits, int decimalPlace, char formatType, int desiredPrecision, NumberFormatInfo nums)
  {
    int wholeDigits = Math.Max(1, decimalPlace), fractionDigits = digits.Length - decimalPlace;
    // trim trailing zeros, which can occur when rendering a power of 10 in exponential format
    while(fractionDigits > 0 && digits[decimalPlace+fractionDigits-1] == 0) fractionDigits--;
    if(desiredPrecision != -1) fractionDigits = Math.Min(desiredPrecision, fractionDigits);

    int length = wholeDigits;
    if(fractionDigits > 0)
    {
      string decimalSymbol = formatType == 'C' ? nums.CurrencyDecimalSeparator :
                             formatType == 'P' ? nums.PercentDecimalSeparator  : nums.NumberDecimalSeparator;
      length += fractionDigits + decimalSymbol.Length;
    }
    return length;
  }

  /// <summary>Computes the rough length of the number if it's rendered in exponential format to the given precision.</summary>
  static int ComputeExponentialLength(byte[] digits, int decimalPlace, char formatType, int desiredPrecision, NumberFormatInfo nums)
  {
    int significandLength =
      ComputeDecimalLength(digits, 1, formatType, desiredPrecision == -1 ? digits.Length-1 : desiredPrecision, nums);
    int absExponent = Math.Abs(decimalPlace - 1);
    return significandLength + 2 + absExponent.ToStringInvariant().Length;
  }

  /// <summary>Returns the first character of the given string, or a nul character if the string is null or empty.</summary>
  static char GetFirstChar(string str)
  {
    return string.IsNullOrEmpty(str) ? '\0' : str[0];
  }

  /// <summary>Returns the last character of the given string, or a nul character if the string is null or empty.</summary>
  static char GetLastChar(string str)
  {
    return string.IsNullOrEmpty(str) ? '\0' : str[str.Length-1];
  }
}

} // namespace AdamMil.Mathematics