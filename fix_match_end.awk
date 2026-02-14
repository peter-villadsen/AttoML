BEGIN { in_match = 0; last_pattern_line = 0 }

# Track when we enter a match expression
/match .* with/ { in_match = 1; match_start = NR }

# Track pattern lines (lines starting with | or the first pattern after with)
in_match && /->/ { last_pattern_line = NR; last_pattern = $0 }

# End match before these terminators if we're in a match
in_match && /^\s*,$/ {
    # Previous line was last pattern, add end
    if (NR == last_pattern_line + 1) {
        lines[last_pattern_line] = lines[last_pattern_line] " end"
    }
    in_match = 0
}

in_match && /^\s*}/ {
    # Previous line was last pattern, add end  
    if (NR == last_pattern_line + 1 && last_pattern_line > 0) {
        lines[last_pattern_line] = lines[last_pattern_line] " end"
    }
    in_match = 0
}

in_match && /^\s*in\s/ {
    # Previous line was last pattern, add end
    if (NR == last_pattern_line + 1) {
        lines[last_pattern_line] = lines[last_pattern_line] " end"
    }
    in_match = 0
}

# Store all lines
{ lines[NR] = $0 }

END {
    for (i = 1; i <= NR; i++) {
        print lines[i]
    }
}
