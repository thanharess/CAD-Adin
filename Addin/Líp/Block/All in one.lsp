; AutoLISP code for AutoCAD - Master Menu for 8 Block Management Tools
; Based on dimdelete.lsp structure: Embedded DCL dialog in TEMP folder
; Functions: 1=BLCOUNT (Dem block), 2=BLERASE (Xoa block), 3=BLSWAP (Hoan doi block), 
;            4=BLBASEPOINT (Doi goc toa do), 5=BLHLAYER (Hien block), 6=BLRENAME (Doi ten block), 
;            7=BLREPLACE (Thay the block), 8=BLEXPLODE (Pha khoi block)
; Load this LISP with (load "blockmaster.lsp") or APPLOAD. Assumes the 8 LSPs are loaded or functions defined.

; Embedded DCL content as string
(defun write-dcl-file (/ dcl-str f temp-path)
  (setq temp-path (strcat (getenv "TEMP") "\\blockmaster.dcl"))
  (if (not temp-path) (setq temp-path "blmaster.dcl"))  ; Fallback
(setq dcl-str
  "blmaster : dialog {
    label = \"Quản lý Block - Menu Chính\";
    width = 45;
    : column {
      : text {
        label = \"Chọn chức năng Block:\";
        alignment = centered;
      }
      : radio_column {
        key = \"options\";
        : radio_button { key = \"bmcount\"; label = \"1. Đếm số lần xuất hiện block (BLCOUNT)\"; }
        : radio_button { key = \"bmerase\"; label = \"2. Xóa tất cả block cùng tên (BLERASE)\"; }
        : radio_button { key = \"bmswap\"; label = \"3. Hoán đổi vị trí 2 block (BLSWAP)\"; }
        : radio_button { key = \"bmbasepoint\"; label = \"4. Đổi gốc tọa độ block (BLBASEPOINT)\"; }
        : radio_button { key = \"bmhlayer\"; label = \"5. Highlight tất cả block cùng tên (BLHLAYER)\"; }
        : radio_button { key = \"bmrename\"; label = \"6. Đổi tên block (BLRENAME)\"; }
        : radio_button { key = \"bmreplace\"; label = \"7. Thay thế nhiều block bằng 1 mẫu (BLREPLACE)\"; }
        : radio_button { key = \"bmexplode\"; label = \"8. Phá khối tất cả block cùng tên (BLEXPLODE)\"; }
      }
      spacer;
      ok_cancel;
    }
  }"
)

  (if (setq f (open temp-path "w"))
    (progn
      (write-line dcl-str f)
      (close f)
      temp-path  ; Return path for load_dialog
    )
    (progn
      (alert (strcat "Không thể tạo file DCL tại " temp-path ". Kiểm tra quyền TEMP folder."))
      nil
    )
  )
)

; Main function
(defun c:blmaster (/ dcl_id dcl-path choice dcl-loaded result)
  (setq choice nil)  ; Initialize
  (setq dcl-loaded (write-dcl-file))
  (if (not dcl-loaded)
    (exit)
  )
  (setq dcl_id (load_dialog dcl-loaded))
  (if (not (new_dialog "blmaster" dcl_id))
    (progn
      (alert "Không load được dialog. Kiểm tra file DCL.")
      (vl-file-delete dcl-loaded)
      (exit)
    )
  )

  ; Select default radio button to #1
  (set_tile "blcount" "1")

  ; Action for OK
  (action_tile "accept"
    "(setq choice (get_tile \"options\"))
     (done_dialog 1)"
  )

  ; Action for Cancel
  (action_tile "cancel" "(done_dialog 0)")

  (setq result (start_dialog))
  (unload_dialog dcl_id)
  (vl-file-delete dcl-loaded)

  (if (= result 1)
    (cond
      ((= choice "blcount")
       (c:BLCOUNT)
      )
      ((= choice "blerase")
       (c:BLERASE)
      )
      ((= choice "blswap")
       (c:BLSWAP)
      )
      ((= choice "blbasepoint")
       (c:BLBASEPOINT)
      )
      ((= choice "blhlayer")
       (c:BLHLAYER)
      )
      ((= choice "blrename")
       (c:BLRENAME)
      )
      ((= choice "blreplace")
       (c:BLREPLACE)
      )
      ((= choice "blexplode")
       (c:BLEXPLODE)
      )
      (T
       (alert "Lựa chọn không hợp lệ. Mặc định chạy BLCOUNT.")
       (c:BLCOUNT)
      )
    )
    (princ "\nĐã hủy lệnh.")
  )
  (princ)
)

; Note: Ensure the 8 functions (c:BLCOUNT, etc.) are defined/loaded from their LSP files before running BLMASTER.
; To auto-load, add (load "path/to/bm_count.lsp") etc. in the main function if needed.

(princ "\nGõ BLMASTER để chạy menu quản lý Block (8 công cụ).")
(princ)