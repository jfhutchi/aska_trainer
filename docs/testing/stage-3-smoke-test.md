# Stage 3 verification

2026-09-14: 46 core tests pass; local Release build succeeds with zero warnings/errors against installed ASKA build 25186770. Installation copies only the two authored DLLs. Stage 2 plugin startup was observed in the main menu with Unknown session blocked; no save was opened. Final combined startup is recorded in the release checklist.

| Check | Result |
| --- | --- |
| Catalog null/empty queries, display/internal matching, deterministic ordering | PASS, 5 pure tests |
| Item search, selection, quantity, native Give Item/Give Stack, full inventory | MANUAL VERIFICATION REQUIRED |
| Given item initialization and save/exit/reload persistence | MANUAL VERIFICATION REQUIRED |
| Durability freeze/resume | Incompatible: loss-only operation unverified |
| Freshness freeze/resume | Incompatible: expiration interval semantics unverified |
| Use effect with unchanged stack, including last item | Incompatible: use-only decrement scope unverified |
| Zero-material craft/native output; normal crafting after disable | Incompatible: isolated material transaction unverified |
| Native persistent free construction | Incompatible: supply/consumption/completion chain unverified |
| Free repair independent of craft/build | Incompatible: isolated repair supply transaction unverified |
| Unknown/co-op blocks give; scene reload clears catalog; no repeating exceptions | MANUAL VERIFICATION REQUIRED |
| Git tracked DLL/EXE/assets/bundles | PASS, empty |

Generated interop wrappers prove signatures, not native behavior. No broad inventory-removal, forced-completion or guessed delta patches were added. Individual unsupported features fail closed while independent work continues. Stage 3 gameplay acceptance remains pending, especially native item persistence.
