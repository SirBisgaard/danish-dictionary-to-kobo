#include "marisa_c_shim.h"
#include <marisa/keyset.h>
#include <marisa/trie.h>
#include <cstring>

extern "C" {
    marisa_builder_t marisa_builder_new(void) {
        try {
            return reinterpret_cast<marisa_builder_t>(new marisa::Keyset());
        } catch (...) {
            return nullptr;
        }
    }

    int marisa_builder_push(
        marisa_builder_t builder,
        const unsigned char* key,
        int length)
    {
        if (!builder || !key || length < 0) return -1;
        auto* ks = reinterpret_cast<marisa::Keyset*>(builder);
        try {
            ks->push_back(reinterpret_cast<const char*>(key),
                          static_cast<size_t>(length));
            return 0;
        } catch (...) {
            return -1;
        }
    }

    marisa_trie_t marisa_builder_build(
        marisa_builder_t builder)
    {
        if (!builder) return nullptr;
        auto* ks = reinterpret_cast<marisa::Keyset*>(builder);
        marisa::Trie* trie = nullptr;
        try {
            trie = new marisa::Trie();
            trie->build(*ks);
        } catch (...) {
            delete trie;
            return nullptr;
        }
        return reinterpret_cast<marisa_trie_t>(trie);
    }

    void marisa_builder_destroy(marisa_builder_t builder) {
        delete reinterpret_cast<marisa::Keyset*>(builder);
    }

    void marisa_trie_save(marisa_trie_t trie, const char* path) {
        if (!trie || !path) return;
        auto* t = reinterpret_cast<marisa::Trie*>(trie);
        try {
            t->save(path);
        } catch (...) { }
    }

    void marisa_trie_destroy(marisa_trie_t trie) {
        delete reinterpret_cast<marisa::Trie*>(trie);
    }
} 
