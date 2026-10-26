# VaxTrack v2.0.0 documentation style guide (locked)

Applies to all four deliverables: Functional Specification, Technical Specification, Test Case Sheet, Project Development Review.

## Page
- US Letter, docx format only (never HTML-to-PDF).
- Margins: 1" all sides.
- Header: none (cover page carries branding). Body pages: none.
- Footer: left = "Confidential — internal review", right = "Page X of Y", separated by a thin top border (0.5pt, #CCCCCC), font Arial 8pt, color #888888.

## Typography
- Font family: Arial throughout (headings and body) — smooth, universally available, no serif/typewriter fallback risk.
- H1 (section): Arial Bold, 22pt, color #222222 (near-black), or reversed white when on a slate cover/band.
- H2 (subsection): Arial Bold, 16pt, color #222222.
- H3 (label/minor heading): Arial Bold, 13pt, color #222222.
- Body paragraph: Arial Regular, 11pt, color #333333, line spacing 1.4.
- Notes/callouts: Arial Italic, 10pt, color #666666, left border 3pt in amber (#BA7517).
- Table header row: Arial Bold, 9pt, white text, fill #5F5E5A (slate).
- Table body: Arial Regular, 9pt, color #222222, alternating row fill white / #F2F1EE.

## Color palette (Option C — slate + amber)
- Primary slate: #5F5E5A (cover band, table headers, diagram box fill)
- Slate dark (text-on-light / connectors): #2C2C2A
- Amber accent: #BA7517 (accent bars, note borders, diagram connectors, highlights)
- Amber light (accent text on slate): #FAEEDA
- Neutral body text: #222222 / #333333
- Muted/footer text: #888888
- Table alt-row fill: #F2F1EE

## Cover page pattern
- Full-width slate (#5F5E5A) band at top third of page.
- "VAXTRACK" wordmark, Arial Bold 9pt, letter-spaced, amber-light (#FAEEDA), top-left of band.
- Document title, Arial Bold 20pt, white.
- One-line subtitle, Arial Italic 10pt, amber-light.
- Document control block below band: version / release date / status / classification / prepared-by as label-value pairs.

## Diagram standard (locked)
Shape language: rounded-corner boxes (radius ~0.14"), thin connectors — this is "style 1" reused with the doc's own palette instead of teal.
- Box fill: slate #5F5E5A, no border.
- Box title text: white, bold, 11pt.
- Box subtitle text: amber-light #FAEEDA or light gray #D3D1C7, 8pt, ≤5 words.
- Connectors: thin (1.3pt) arrows, color slate-dark #2C2C2A (not amber — amber is reserved for accents/notes so it doesn't compete visually with connector lines).
- Use amber only for a single emphasis element per diagram if needed (e.g. a highlighted decision box or the note border), never for routine connectors — keeps the palette from feeling busy.
- Apply identically across HLD, LLD, sequence, state, and flow diagrams in every document.

## Source of truth
Derived from `01-Functional-Specification-Document.pdf` cover/table styling, revised per user feedback (font swapped from Calibri to Arial; diagram reverted to rounded-box thin-connector shape recolored to slate+amber instead of teal).
