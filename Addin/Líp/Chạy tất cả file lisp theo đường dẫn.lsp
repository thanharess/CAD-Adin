;;;===========================================================
;;; LỆNH: MYLOADLISP
;;; Tác dụng: Load các file LISP theo danh sách đường dẫn định sẵn
;;;===========================================================

(defun c:MYLOADLISP (/ files file loaded count)
  (vl-load-com)
  (prompt "\nLỆNH: MYLOADLISP - Load các file Lisp theo danh sách định sẵn.")

  ;; ----- KHAI BÁO DANH SÁCH FILE CẦN LOAD -----
  ;; Bạn chỉ cần sửa danh sách này theo đường dẫn riêng của mình
  (setq files
    '(
      "C:\\Users\\thanh.tke\\Desktop\\Template\\Cad lisp\\Cad lisp\\file gồm cả dcl\\changelayer.lsp"
      "C:\\Users\\thanh.tke\\Desktop\\Template\\Cad lisp\\Cad lisp\\file gồm cả dcl\\dimdelete.lsp"
      "C:\\Users\\thanh.tke\\Desktop\\Template\\Cad lisp\\Cad lisp\\file gồm cả dcl\\dimtextstylechange.lsp"
      "C:\\Users\\thanh.tke\\Desktop\\Template\\Cad lisp\\Cad lisp\\file gồm cả dcl\\Layerdelete.lsp"
    )
  )

  ;; ----- BẮT ĐẦU LOAD -----
  (setq loaded 0 count (length files))
  (princ (strcat "\nĐang load " (itoa count) " file LISP..."))

  (foreach file files
    (if (findfile file)
      (if (not (vl-catch-all-error-p (vl-catch-all-apply 'load (list file nil))))
        (progn
          (setq loaded (1+ loaded))
          (princ (strcat "\n→ Đã load: " file))
        )
        (princ (strcat "\n→ Lỗi load: " file))
      )
      (princ (strcat "\n→ Không tìm thấy file: " file))
    )
  )

  (princ (strcat "\nHoàn thành: Load thành công "
                 (itoa loaded) "/" (itoa count) " file."))
  (princ)
)

(princ "\nGõ MYLOADLISP để chạy lệnh.")
(princ)
