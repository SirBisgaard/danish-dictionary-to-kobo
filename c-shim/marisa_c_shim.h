#ifndef MARISA_C_SHIM_H
#define MARISA_C_SHIM_H

#include <stddef.h>
#ifdef __cplusplus
extern "C" {
#endif

typedef void* marisa_builder_t;
typedef void* marisa_trie_t;

marisa_builder_t marisa_builder_new(void);

int               marisa_builder_push(
                     marisa_builder_t builder,
                     const unsigned char* key,
                     int length);

marisa_trie_t     marisa_builder_build(
                     marisa_builder_t builder);

void              marisa_builder_destroy(
                     marisa_builder_t builder);

void marisa_trie_save(
       marisa_trie_t trie,
       const char* path);

void marisa_trie_destroy(
      marisa_trie_t trie);

#ifdef __cplusplus
}
#endif
#endif

