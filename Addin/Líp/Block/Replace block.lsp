;;; ============================================================
;;; BM_ReplaceBlocks_Advanced.lsp
;;; Thay thế nhiều block khác nhau bằng 1 block mẫu
;;; Giữ nguyên vị trí, góc xoay, tỉ lệ
;;; Có thể chọn block mẫu từ danh sách hoặc trên bản vẽ
;;; Có tùy chọn xóa block mẫu sau khi chọn
;;; ============================================================

(vl-load-com)

(defun bm-print (s) (princ (strcat "\n" s)))

(defun bm-get-block-names (/ doc blocks blockNames)
  (setq doc (vla-get-ActiveDocument (vlax-get-acad-object)))
  (setq blocks (vla-get-Blocks doc))
  (setq blockNames '())
  (vlax-for blk blocks
    (if (and (= (vla-get-IsLayout blk) :vlax-false)
             (/= (vla-get-Name blk) "*Model_Space")
             (/= (vla-get-Name blk) "*Paper_Space"))
      (setq blockNames (cons (vla-get-Name blk) blockNames))
    )
  )
  (reverse blockNames)
)

(defun c:BLREPLACE (/ refBlkName refBlk ed ss i en ed2 insPt rot sx sy sz doc space delSample opt blkList userSel)
  (princ "\n=== BM Replace Blocks (Advanced) ===")

  ;; B1: Chọn cách lấy block mẫu
  (initget "Pick List")
  (setq opt (getkword "\nChọn block mẫu bằng: [Pick/List] <Pick>: "))
  (if (null opt) (setq opt "Pick"))

  (cond
    ;; --- Chọn block mẫu bằng PICK ---
    ((= opt "Pick")
      (setq refBlk (entsel "\nChọn block mẫu để thay thế: "))
      (if (not refBlk)
        (progn (bm-print "❌ Bạn đã hủy chọn.") (exit))
      )
      (setq ed (entget (car refBlk)))
      (if (/= (cdr (assoc 0 ed)) "INSERT")
        (progn (bm-print "❌ Đối tượng được chọn không phải block.") (exit))
      )
      (setq refBlkName (cdr (assoc 2 ed)))

      ;; Hỏi có xóa block mẫu không
      (initget "Yes No")
      (setq delSample (getkword "\nBạn có muốn xóa block mẫu sau khi thay thế không? [Yes/No] <No>: "))
      (if (or (null delSample) (= delSample "No"))
        (setq delSample nil)
        (setq delSample T)
      )
    )

    ;; --- Chọn block mẫu từ danh sách ---
    ((= opt "List")
      (setq blkList (bm-get-block-names))
      (if (null blkList)
        (progn (bm-print "❌ Bản vẽ không có block nào!") (exit))
      )
      (princ "\nCác block trong bản vẽ:")
      (foreach n blkList (princ (strcat "\n  - " n)))
      (setq userSel (getstring "\nNhập tên block mẫu muốn dùng: "))
      (if (not (member userSel blkList))
        (progn (bm-print "❌ Tên block không tồn tại trong bản vẽ.") (exit))
      )
      (setq refBlkName userSel)
    )
  )

  (bm-print (strcat "🔹 Block mẫu: " refBlkName))

  ;; B2: Chọn block cần thay
  (setq ss (ssget "_:L" '((0 . "INSERT"))))
  (if (not ss)
    (progn (bm-print "❌ Không chọn block nào để thay.") (exit))
  )

  (setq doc (vla-get-ActiveDocument (vlax-get-acad-object)))
  (setq space (if (= (getvar "CVPORT") 1)
                (vla-get-PaperSpace doc)
                (vla-get-ModelSpace doc)))

  (setq i 0)
  (repeat (sslength ss)
    (setq en (ssname ss i))
    (setq ed2 (entget en))
    (if (= (cdr (assoc 0 ed2)) "INSERT")
      (progn
        (setq insPt (cdr (assoc 10 ed2)))
        (setq rot (cdr (assoc 50 ed2)))
        (setq sx (cdr (assoc 41 ed2)))
        (setq sy (cdr (assoc 42 ed2)))
        (setq sz (cdr (assoc 43 ed2)))

        ;; Tạo block mới
        (vla-InsertBlock space
          (vlax-3d-point insPt)
          refBlkName
          (if sx sx 1.0)
          (if sy sy 1.0)
          (if sz sz 1.0)
          (if rot rot 0.0)
        )

        ;; Xóa block cũ
        (entdel en)
      )
    )
    (setq i (1+ i))
  )

  ;; Nếu có chọn block mẫu thì xóa nó
  (if (and delSample refBlk)
    (entdel (car refBlk))
  )

  (bm-print (strcat "✅ Đã thay thế " (itoa (sslength ss)) " block bằng \"" refBlkName "\"."))
  (princ)
)

(princ "\nGõ BLREPLACE để chạy lệnh thay thế block nâng cao.")
(princ)
